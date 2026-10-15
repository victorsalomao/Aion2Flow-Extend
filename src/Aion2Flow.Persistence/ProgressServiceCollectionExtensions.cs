using Cloris.Aion2Flow.Domain.Progress;
using Microsoft.Extensions.DependencyInjection;

namespace Cloris.Aion2Flow.Persistence;

public static class ProgressServiceCollectionExtensions
{
    /// <summary>Registra repositorios SQLite, catalogos JSON, ResetClock, checklist e timers de eventos.</summary>
    public static IServiceCollection AddProgressDomain(this IServiceCollection services,
        string databasePath, string dataDirectory, ServerRegion region)
    {
        services.AddSingleton(_ =>
        {
            var dir = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            return new SqliteDatabase(databasePath);
        });
        services.AddSingleton<IAltRepository, SqliteAltRepository>();
        services.AddSingleton<ITaskRepository, SqliteTaskRepository>();
        services.AddSingleton<ITaskProgressRepository, SqliteTaskProgressRepository>();
        services.AddSingleton<IResourceRepository, SqliteResourceRepository>();
        services.AddSingleton<IManualFieldRepository, SqliteManualFieldRepository>();
        services.AddSingleton<ITaskCatalog>(_ => TaskCatalog.Load(Path.Combine(dataDirectory, "tasks.json")));
        services.AddSingleton<IEventCatalog>(_ => EventCatalog.Load(Path.Combine(dataDirectory, "events.json")));
        services.AddSingleton<IResetClock>(_ => new ResetClock(region));
        services.AddSingleton<IChecklistService>(sp => new ChecklistService(
            sp.GetRequiredService<ITaskCatalog>(), sp.GetRequiredService<ITaskProgressRepository>(), sp.GetRequiredService<IResetClock>()));
        services.AddSingleton<IEventTimerService>(sp =>
            new EventTimerService(sp.GetRequiredService<IEventCatalog>(), ResetRule.For(region)));
        return services;
    }
}
