using Cloris.Aion2Flow.Domain.Progress;
using Microsoft.Data.Sqlite;

namespace Cloris.Aion2Flow.Persistence;

public sealed class SqliteAltRepository(SqliteDatabase db) : IAltRepository
{
    private const string Select = "SELECT id, name, server, class, level FROM alts";

    private static Alt Map(SqliteDataReader r) =>
        new(new AltId(Guid.Parse(r.GetString(0))), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4));

    public Task<IReadOnlyList<Alt>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Alt>>(db.Query(Select + " ORDER BY name;", _ => { }, Map));

    public Task<Alt?> GetAsync(AltId id, CancellationToken ct = default)
    {
        var l = db.Query(Select + " WHERE id = $id;", p => p.AddWithValue("$id", id.Value.ToString()), Map);
        return Task.FromResult(l.Count > 0 ? l[0] : null);
    }

    public Task SaveAsync(Alt a, CancellationToken ct = default)
    {
        db.Run("INSERT INTO alts (id, name, server, class, level) VALUES ($id, $n, $s, $c, $l) " +
               "ON CONFLICT(id) DO UPDATE SET name=$n, server=$s, class=$c, level=$l;", p =>
        {
            p.AddWithValue("$id", a.Id.Value.ToString());
            p.AddWithValue("$n", a.Name);
            p.AddWithValue("$s", a.Server);
            p.AddWithValue("$c", a.Class);
            p.AddWithValue("$l", a.Level);
        });
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AltId id, CancellationToken ct = default)
    {
        db.Run("DELETE FROM alts WHERE id = $id;", p => p.AddWithValue("$id", id.Value.ToString()));
        return Task.CompletedTask;
    }
}

public sealed class SqliteTaskRepository(SqliteDatabase db) : ITaskRepository
{
    public Task<IReadOnlyList<ProgressTask>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProgressTask>>(db.Query(
            "SELECT id, name, cadence, target FROM tasks ORDER BY name;", _ => { },
            r => new ProgressTask(new TaskId(Guid.Parse(r.GetString(0))), r.GetString(1), (TaskCadence)r.GetInt32(2), r.GetInt32(3))));

    public Task SaveAsync(ProgressTask t, CancellationToken ct = default)
    {
        db.Run("INSERT INTO tasks (id, name, cadence, target) VALUES ($id, $n, $c, $t) " +
               "ON CONFLICT(id) DO UPDATE SET name=$n, cadence=$c, target=$t;", p =>
        {
            p.AddWithValue("$id", t.Id.Value.ToString());
            p.AddWithValue("$n", t.Name);
            p.AddWithValue("$c", (int)t.Cadence);
            p.AddWithValue("$t", t.Target);
        });
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskId id, CancellationToken ct = default)
    {
        db.Run("DELETE FROM task_progress WHERE task_id = $id;", p => p.AddWithValue("$id", id.Value.ToString()));
        db.Run("DELETE FROM tasks WHERE id = $id;", p => p.AddWithValue("$id", id.Value.ToString()));
        return Task.CompletedTask;
    }
}

public sealed class SqliteTaskProgressRepository(SqliteDatabase db) : ITaskProgressRepository
{
    public Task<IReadOnlyList<TaskProgress>> ListByAltAsync(AltId altId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TaskProgress>>(db.Query(
            "SELECT task_id, current, updated_at FROM task_progress WHERE alt_id = $a;",
            p => p.AddWithValue("$a", altId.Value.ToString()),
            r => new TaskProgress(altId, new TaskId(Guid.Parse(r.GetString(0))), r.GetInt32(1),
                DateTimeOffset.FromUnixTimeMilliseconds(r.GetInt64(2)))));

    public Task SaveAsync(TaskProgress t, CancellationToken ct = default)
    {
        db.Run("INSERT INTO task_progress (alt_id, task_id, current, updated_at) VALUES ($a, $t, $c, $u) " +
               "ON CONFLICT(alt_id, task_id) DO UPDATE SET current=$c, updated_at=$u;", p =>
        {
            p.AddWithValue("$a", t.AltId.Value.ToString());
            p.AddWithValue("$t", t.TaskId.Value.ToString());
            p.AddWithValue("$c", t.Current);
            p.AddWithValue("$u", t.UpdatedAt.ToUnixTimeMilliseconds());
        });
        return Task.CompletedTask;
    }
}

public sealed class SqliteResourceRepository(SqliteDatabase db) : IResourceRepository
{
    public Task<IReadOnlyList<Resource>> ListByAltAsync(AltId altId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Resource>>(db.Query(
            "SELECT key, current, max, updated_at FROM resources WHERE alt_id = $a ORDER BY key;",
            p => p.AddWithValue("$a", altId.Value.ToString()),
            r => new Resource(altId, r.GetString(0), r.GetInt32(1), r.GetInt32(2),
                DateTimeOffset.FromUnixTimeMilliseconds(r.GetInt64(3)))));

    public Task SaveAsync(Resource r, CancellationToken ct = default)
    {
        db.Run("INSERT INTO resources (alt_id, key, current, max, updated_at) VALUES ($a, $k, $c, $m, $u) " +
               "ON CONFLICT(alt_id, key) DO UPDATE SET current=$c, max=$m, updated_at=$u;", p =>
        {
            p.AddWithValue("$a", r.AltId.Value.ToString());
            p.AddWithValue("$k", r.Key);
            p.AddWithValue("$c", r.Current);
            p.AddWithValue("$m", r.Max);
            p.AddWithValue("$u", r.UpdatedAt.ToUnixTimeMilliseconds());
        });
        return Task.CompletedTask;
    }
}
