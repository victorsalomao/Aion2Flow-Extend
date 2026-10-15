using System.Security.Cryptography;
using System.Text;

namespace Cloris.Aion2Flow.Domain.Progress;

public enum ChecklistState { Pending, Done, Reset }

public sealed record ChecklistItem(TaskDefinition Task, int Current, ChecklistState State, DateTimeOffset NextReset)
{
    public bool IsDone => State == ChecklistState.Done;
}

public interface IChecklistService
{
    /// <summary>Estado atual; progresso anterior ao ultimo reset conta como zerado.</summary>
    Task<IReadOnlyList<ChecklistItem>> GetAsync(AltId alt, CancellationToken ct = default);

    /// <summary>Define a contagem (0..maxCount). Desmarcar = 0.</summary>
    Task<ChecklistItem?> SetCountAsync(AltId alt, string taskId, int count, CancellationToken ct = default);

    /// <summary>Alterna entre concluido (maxCount) e pendente (0).</summary>
    Task<ChecklistItem?> ToggleAsync(AltId alt, string taskId, CancellationToken ct = default);

    event Action<AltId, string>? Changed;
}

/// <summary>Maquina de estados Pendente -> Feito -> Resetado, avaliada de forma lazy pelo ResetClock.</summary>
public sealed class ChecklistService(ITaskCatalog catalog, ITaskProgressRepository repo, IResetClock clock, TimeProvider? time = null) : IChecklistService
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public event Action<AltId, string>? Changed;

    public static TaskId KeyFor(string taskId) =>
        new(new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(taskId)).AsSpan(0, 16)));

    public async Task<IReadOnlyList<ChecklistItem>> GetAsync(AltId alt, CancellationToken ct = default)
    {
        var saved = (await repo.ListByAltAsync(alt, ct)).ToDictionary(p => p.TaskId);
        var list = new List<ChecklistItem>(catalog.Tasks.Count);
        foreach (var t in catalog.Tasks)
        {
            saved.TryGetValue(KeyFor(t.Id), out var p);
            list.Add(Build(t, p));
        }
        return list;
    }

    public async Task<ChecklistItem?> SetCountAsync(AltId alt, string taskId, int count, CancellationToken ct = default)
    {
        var t = catalog.Tasks.FirstOrDefault(x => x.Id == taskId);
        if (t is null) return null;
        var p = new TaskProgress(alt, KeyFor(taskId), Math.Clamp(count, 0, t.MaxCount), _time.GetUtcNow());
        await repo.SaveAsync(p, ct);
        Changed?.Invoke(alt, taskId);
        return Build(t, p);
    }

    public async Task<ChecklistItem?> ToggleAsync(AltId alt, string taskId, CancellationToken ct = default)
    {
        var t = catalog.Tasks.FirstOrDefault(x => x.Id == taskId);
        if (t is null) return null;
        var cur = (await GetAsync(alt, ct)).First(i => i.Task.Id == taskId);
        return await SetCountAsync(alt, taskId, cur.IsDone ? 0 : t.MaxCount, ct);
    }

    private ChecklistItem Build(TaskDefinition t, TaskProgress? p)
    {
        var weekly = t.Cadence == TaskCadence.Weekly;
        var lastReset = weekly ? clock.LastWeekly() : clock.LastDaily();
        var next = weekly ? clock.NextWeekly() : clock.NextDaily();
        if (p is null) return new(t, 0, ChecklistState.Pending, next);
        if (p.UpdatedAt < lastReset) return new(t, 0, ChecklistState.Reset, next);
        var cur = Math.Min(p.Current, t.MaxCount);
        return new(t, cur, cur >= t.MaxCount ? ChecklistState.Done : ChecklistState.Pending, next);
    }
}
