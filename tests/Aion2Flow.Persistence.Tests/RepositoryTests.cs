using Cloris.Aion2Flow.Domain.Progress;
using Xunit;

namespace Cloris.Aion2Flow.Persistence.Tests;

public class RepositoryTests
{
    private static readonly DateTimeOffset T = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);

    [Fact]
    public void Migration_sets_version()
    {
        using var db = SqliteDatabase.InMemory();
        Assert.Equal(2, db.SchemaVersion);
    }

    [Fact]
    public async Task Alt_crud()
    {
        using var db = SqliteDatabase.InMemory();
        var repo = new SqliteAltRepository(db);
        var a = new Alt(AltId.New(), "Zed", "Siel", "Gladiator", 55);
        await repo.SaveAsync(a);
        Assert.Equal(a, await repo.GetAsync(a.Id));
        await repo.SaveAsync(a.Rename("Zed2"));
        Assert.Equal("Zed2", (await repo.ListAsync()).Single().Name);
        await repo.DeleteAsync(a.Id);
        Assert.Null(await repo.GetAsync(a.Id));
        Assert.Empty(await repo.ListAsync());
    }

    [Fact]
    public async Task Task_crud_and_delete_removes_progress()
    {
        using var db = SqliteDatabase.InMemory();
        var alt = new Alt(AltId.New(), "A", "S", "C", 1);
        await new SqliteAltRepository(db).SaveAsync(alt);
        var tasks = new SqliteTaskRepository(db);
        var prog = new SqliteTaskProgressRepository(db);
        var t = new ProgressTask(TaskId.New(), "Dungeon", TaskCadence.Weekly, 3);
        await tasks.SaveAsync(t);
        await tasks.SaveAsync(t with { Target = 5 });
        Assert.Equal(t with { Target = 5 }, (await tasks.ListAsync()).Single());
        await prog.SaveAsync(new TaskProgress(alt.Id, t.Id, 2, T));
        await prog.SaveAsync(new TaskProgress(alt.Id, t.Id, 3, T));
        Assert.Equal(3, (await prog.ListByAltAsync(alt.Id)).Single().Current);
        await tasks.DeleteAsync(t.Id);
        Assert.Empty(await tasks.ListAsync());
        Assert.Empty(await prog.ListByAltAsync(alt.Id));
    }

    [Fact]
    public async Task Resource_upsert_and_cascade_on_alt_delete()
    {
        using var db = SqliteDatabase.InMemory();
        var alt = new Alt(AltId.New(), "A", "S", "C", 1);
        var alts = new SqliteAltRepository(db);
        var res = new SqliteResourceRepository(db);
        await alts.SaveAsync(alt);
        await res.SaveAsync(new Resource(alt.Id, "energy", 10, 100, T));
        await res.SaveAsync(new Resource(alt.Id, "energy", 20, 100, T));
        Assert.Equal(new Resource(alt.Id, "energy", 20, 100, T), (await res.ListByAltAsync(alt.Id)).Single());
        await alts.DeleteAsync(alt.Id);
        Assert.Empty(await res.ListByAltAsync(alt.Id));
    }

    [Fact]
    public async Task Persists_across_reopen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"a2f-{Guid.NewGuid():N}.db");
        var a = new Alt(AltId.New(), "P", "S", "C", 9);
        try
        {
            using (var db = new SqliteDatabase(path)) await new SqliteAltRepository(db).SaveAsync(a);
            using (var db = new SqliteDatabase(path))
            {
                Assert.Equal(2, db.SchemaVersion);
                Assert.Equal(a, await new SqliteAltRepository(db).GetAsync(a.Id));
            }
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
    }
}
