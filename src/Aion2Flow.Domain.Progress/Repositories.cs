namespace Cloris.Aion2Flow.Domain.Progress;

public interface IAltRepository
{
    Task<IReadOnlyList<Alt>> ListAsync(CancellationToken ct = default);
    Task<Alt?> GetAsync(AltId id, CancellationToken ct = default);
    Task SaveAsync(Alt alt, CancellationToken ct = default);
    Task DeleteAsync(AltId id, CancellationToken ct = default);
}

public interface ITaskRepository
{
    Task<IReadOnlyList<ProgressTask>> ListAsync(CancellationToken ct = default);
    Task SaveAsync(ProgressTask task, CancellationToken ct = default);
    Task DeleteAsync(TaskId id, CancellationToken ct = default);
}

public interface ITaskProgressRepository
{
    Task<IReadOnlyList<TaskProgress>> ListByAltAsync(AltId altId, CancellationToken ct = default);
    Task SaveAsync(TaskProgress progress, CancellationToken ct = default);
}

public interface IResourceRepository
{
    Task<IReadOnlyList<Resource>> ListByAltAsync(AltId altId, CancellationToken ct = default);
    Task SaveAsync(Resource resource, CancellationToken ct = default);
}

public interface IProgressEventSink
{
    ValueTask PublishAsync(ProgressEvent evt, CancellationToken ct = default);
}
