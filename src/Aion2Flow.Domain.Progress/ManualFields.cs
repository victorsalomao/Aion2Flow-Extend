namespace Cloris.Aion2Flow.Domain.Progress;

/// <summary>Campo numerico manual por alt (ex.: kinah, pontos). Rastreia o valor no inicio do dia para o delta "hoje".</summary>
public sealed record ManualField(AltId AltId, string Key, long Value, long DayStartValue, DateTimeOffset DayStartAt, DateTimeOffset UpdatedAt)
{
    public static ManualField Create(AltId altId, string key, long value, DateTimeOffset now, DateTimeOffset lastDailyReset) =>
        new(altId, key, value, value, lastDailyReset, now);

    /// <summary>Atualiza o valor; se um reset diario ocorreu desde a ultima base, rebaseia com o valor anterior.</summary>
    public ManualField Set(long value, DateTimeOffset now, DateTimeOffset lastDailyReset) =>
        Rebase(lastDailyReset) with { Value = value, UpdatedAt = now };

    public long TodayDelta(DateTimeOffset lastDailyReset)
    {
        var f = Rebase(lastDailyReset);
        return f.Value - f.DayStartValue;
    }

    private ManualField Rebase(DateTimeOffset lastDailyReset) =>
        DayStartAt < lastDailyReset ? this with { DayStartValue = Value, DayStartAt = lastDailyReset } : this;
}

public interface IManualFieldRepository
{
    Task<IReadOnlyList<ManualField>> ListByAltAsync(AltId altId, CancellationToken ct = default);
    Task SaveAsync(ManualField field, CancellationToken ct = default);
    Task DeleteAsync(AltId altId, string key, CancellationToken ct = default);
}

public sealed class ManualFieldService(IManualFieldRepository repo, IResetClock clock, TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<ManualField> SetAsync(AltId altId, string key, long value, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var reset = clock.LastDaily();
        var existing = (await repo.ListByAltAsync(altId, ct)).FirstOrDefault(f => f.Key == key);
        var f = existing is null ? ManualField.Create(altId, key, value, now, reset) : existing.Set(value, now, reset);
        await repo.SaveAsync(f, ct);
        return f;
    }

    public async Task<long> TodayDeltaAsync(AltId altId, string key, CancellationToken ct = default)
    {
        var f = (await repo.ListByAltAsync(altId, ct)).FirstOrDefault(x => x.Key == key);
        return f?.TodayDelta(clock.LastDaily()) ?? 0;
    }
}
