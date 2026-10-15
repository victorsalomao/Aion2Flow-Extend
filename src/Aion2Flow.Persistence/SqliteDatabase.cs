using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Cloris.Aion2Flow.Persistence;

/// <summary>Conexão SQLite única e serializada, com migrations por PRAGMA user_version.</summary>
public sealed class SqliteDatabase : IDisposable
{
    private static readonly string[] Migrations =
    [
        """
        CREATE TABLE alts (id TEXT PRIMARY KEY, name TEXT NOT NULL, server TEXT NOT NULL, class TEXT NOT NULL, level INTEGER NOT NULL);
        CREATE TABLE tasks (id TEXT PRIMARY KEY, name TEXT NOT NULL, cadence INTEGER NOT NULL, target INTEGER NOT NULL);
        CREATE TABLE task_progress (alt_id TEXT NOT NULL REFERENCES alts(id) ON DELETE CASCADE, task_id TEXT NOT NULL, current INTEGER NOT NULL, updated_at INTEGER NOT NULL, PRIMARY KEY (alt_id, task_id));
        CREATE TABLE resources (alt_id TEXT NOT NULL REFERENCES alts(id) ON DELETE CASCADE, key TEXT NOT NULL, current INTEGER NOT NULL, max INTEGER NOT NULL, updated_at INTEGER NOT NULL, PRIMARY KEY (alt_id, key));
        """,
        """
        CREATE TABLE manual_fields (alt_id TEXT NOT NULL REFERENCES alts(id) ON DELETE CASCADE, key TEXT NOT NULL, value INTEGER NOT NULL, day_start_value INTEGER NOT NULL, day_start_at INTEGER NOT NULL, updated_at INTEGER NOT NULL, PRIMARY KEY (alt_id, key));
        """,
    ];

    private readonly SqliteConnection _connection;
    private readonly object _gate = new();

    public SqliteDatabase(string dataSource)
    {
        _connection = new SqliteConnection($"Data Source={dataSource}");
        _connection.Open();
        Execute("PRAGMA foreign_keys = ON;");
        Migrate();
    }

    public static SqliteDatabase InMemory() => new(":memory:");

    public int SchemaVersion
    {
        get
        {
            lock (_gate)
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "PRAGMA user_version;";
                return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }
    }

    private void Migrate()
    {
        for (var v = SchemaVersion; v < Migrations.Length; v++)
        {
            lock (_gate)
            {
                using var tx = _connection.BeginTransaction();
                using var cmd = _connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = Migrations[v] + $"PRAGMA user_version = {v + 1};";
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
        }
    }

    private void Execute(string sql)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    internal void Run(string sql, Action<SqliteParameterCollection> bind)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            bind(cmd.Parameters);
            cmd.ExecuteNonQuery();
        }
    }

    internal List<T> Query<T>(string sql, Action<SqliteParameterCollection> bind, Func<SqliteDataReader, T> map)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            bind(cmd.Parameters);
            using var r = cmd.ExecuteReader();
            var list = new List<T>();
            while (r.Read()) list.Add(map(r));
            return list;
        }
    }

    public void Dispose() => _connection.Dispose();
}
