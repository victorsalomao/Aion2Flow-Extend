using Cloris.Aion2Flow.Domain.Progress;
using Cloris.Aion2Flow.Services.Logging;

namespace Cloris.Aion2Flow.Services.Reminders;

/// <summary>Verifica resina cheia e eventos proximos em background (45 s), sem depender do painel aberto.</summary>
public sealed class ProgressReminderService : IDisposable
{
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(45);

    private readonly IAltRepository _alts;
    private readonly IManualFieldRepository _fields;
    private readonly ResinMonitor _resin;
    private readonly EventReminderMonitor _events;
    private readonly Dictionary<AltId, RechargeRule> _rules = [];
    private readonly CancellationTokenSource _cts = new();
    private int _started;

    public ProgressReminderService(
        IAltRepository alts,
        IManualFieldRepository fields,
        IResourceRepository resources,
        IEventTimerService timers,
        AlertToastService notifier)
    {
        _alts = alts;
        _fields = fields;
        _resin = new ResinMonitor(resources, notifier, RuleFor);
        _events = new EventReminderMonitor(timers, notifier);
    }

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 0)
        {
            _ = Task.Run(() => RunAsync(_cts.Token));
        }
    }

    private RechargeRule RuleFor(AltId id) =>
        _rules.TryGetValue(id, out var rule) ? rule : RechargeRule.Default(ResinFields.Key);

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Period);
        do
        {
            try
            {
                var alts = await _alts.ListAsync(ct);
                foreach (var alt in alts)
                {
                    _rules[alt.Id] = ResinFields.BuildRule(await _fields.ListByAltAsync(alt.Id, ct));
                }

                await _resin.CheckAsync(alts.Select(a => a.Id).ToList(), ct);
                await _events.CheckAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
            {
                AppLog.Write(AppLogLevel.Warning, $"[Reminders] falha na verificacao: {ex.Message}");
            }
        }
        while (await WaitAsync(timer, ct));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    public void Dispose() => _cts.Cancel();
}
