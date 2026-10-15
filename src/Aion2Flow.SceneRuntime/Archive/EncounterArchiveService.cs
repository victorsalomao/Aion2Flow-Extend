using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Cloris.Aion2Flow.SceneRuntime.Combat;
namespace Cloris.Aion2Flow.SceneRuntime.Archive;

public sealed class EncounterArchiveService
{
    public const int MaxHistoryCount = 10;

    private readonly Lock _lock = new();
    private readonly List<ArchivedEncounterRecord> _history = [];
    private readonly Dictionary<Guid, ArchivedEncounterRecord> _historyByEncounterId = [];
    private ImmutableArray<ArchivedEncounterRecord> _historySnapshot = [];
    private readonly Func<bool>? _spillEnabled;
    private readonly string? _spillDirectory;

    public EncounterArchiveService()
    {
    }

    /// <param name="spillEnabled">Evaluated per archive (LiteMode flag).</param>
    /// <param name="spillBaseDirectory">Base folder; a per-instance subfolder is created lazily. Defaults to the temp folder.</param>
    public EncounterArchiveService(Func<bool> spillEnabled, string? spillBaseDirectory = null)
    {
        _spillEnabled = spillEnabled;
        _spillDirectory = Path.Combine(spillBaseDirectory ?? Path.Combine(Path.GetTempPath(), "Aion2Flow", "archive"), Guid.NewGuid().ToString("N"));
        CleanStaleDirectories(Path.GetDirectoryName(_spillDirectory)!);
    }

    public event EventHandler? HistoryChanged;

    public IReadOnlyList<ArchivedEncounterRecord> History => _historySnapshot;

    public ArchivedEncounterRecord? Archive(SceneArchivePayload payload, string trigger, bool isAutomatic)
    {
        return AddArchiveRecord(payload, trigger, isAutomatic);
    }

    private ArchivedEncounterRecord? AddArchiveRecord(SceneArchivePayload scenePayload, string trigger, bool isAutomatic)
    {
        var archivedSnapshot = scenePayload.Snapshot;
        if (archivedSnapshot.EncounterTime <= 0 || archivedSnapshot.EncounterEndTime < archivedSnapshot.EncounterStartTime || archivedSnapshot.Combatants.Count == 0)
        {
            return null;
        }

        ArchivedEncounterRecord? record;
        bool historyChanged;
        List<ArchivedEncounterRecord>? evicted = null;
        lock (_lock)
        {
            if (_history.Count > 0 && IsEquivalent(_history[0].ScenePayload.Snapshot, archivedSnapshot))
            {
                return null;
            }

            record = new ArchivedEncounterRecord
            {
                ArchivedAt = scenePayload.SceneStarted.ToLocalTime(),
                Trigger = trigger,
                IsAutomatic = isAutomatic,
                ScenePayload = scenePayload
            };

            _history.Insert(0, record);
            _historyByEncounterId[archivedSnapshot.EncounterId] = record;
            if (_history.Count > MaxHistoryCount)
            {
                for (var i = MaxHistoryCount; i < _history.Count; i++)
                {
                    (evicted ??= []).Add(_history[i]);
                    _historyByEncounterId.Remove(_history[i].ScenePayload.Snapshot.EncounterId);
                }

                _history.RemoveRange(MaxHistoryCount, _history.Count - MaxHistoryCount);
            }

            _historySnapshot = [.. _history];
            historyChanged = true;
        }

        if (_spillEnabled?.Invoke() == true && _spillDirectory is not null)
            record.ScenePayload.TrySpill(Path.Combine(_spillDirectory, record.Id.ToString("N") + ".asf"));

        if (evicted is not null)
        {
            foreach (var old in evicted)
                old.ScenePayload.DeleteSpill();
        }

        if (historyChanged)
        {
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        return record;
    }

    public bool TryGetEncounter(Guid encounterId, [NotNullWhen(true)] out ArchivedEncounterRecord? record)
    {
        lock (_lock)
        {
            return _historyByEncounterId.TryGetValue(encounterId, out record);
        }
    }

    private static void CleanStaleDirectories(string root)
    {
        try
        {
            if (!Directory.Exists(root))
                return;

            var cutoff = DateTime.UtcNow.AddHours(-12);
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                if (Directory.GetLastWriteTimeUtc(dir) < cutoff)
                    Directory.Delete(dir, true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool IsEquivalent(SceneCombatSnapshot left, SceneCombatSnapshot right)
    {
        return left.EncounterTime == right.EncounterTime &&
               left.EncounterId == right.EncounterId &&
               left.Encounter.TrackingTargetId == right.Encounter.TrackingTargetId &&
               (left.TargetObservation?.InstanceId ?? 0) == (right.TargetObservation?.InstanceId ?? 0) &&
               left.MapId == right.MapId &&
               left.MapInstanceId == right.MapInstanceId &&
               left.Combatants.Count == right.Combatants.Count &&
               SumDamage(left) == SumDamage(right);
    }

    private static double SumDamage(SceneCombatSnapshot snapshot)
    {
        var totalDamage = 0d;
        var combatants = snapshot.Combatants.AsSpan();
        foreach (ref readonly var entry in combatants)
        {
            totalDamage += entry.Metrics.DamageAmount;
        }

        return totalDamage;
    }
}
