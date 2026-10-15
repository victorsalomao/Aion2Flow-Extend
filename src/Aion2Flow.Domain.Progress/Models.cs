namespace Cloris.Aion2Flow.Domain.Progress;

public readonly record struct AltId(Guid Value)
{
    public static AltId New() => new(Guid.NewGuid());
}

public readonly record struct TaskId(Guid Value)
{
    public static TaskId New() => new(Guid.NewGuid());
}

public enum TaskCadence { Daily, Weekly, OneTime }

public sealed record Alt(AltId Id, string Name, string Server, string Class, int Level)
{
    public Alt Rename(string name) => this with { Name = name };
}

public sealed record ProgressTask(TaskId Id, string Name, TaskCadence Cadence, int Target)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && Target > 0;
}

public sealed record TaskProgress(AltId AltId, TaskId TaskId, int Current, DateTimeOffset UpdatedAt)
{
    public TaskProgress Advance(int amount, int target, DateTimeOffset now) =>
        this with { Current = Math.Clamp(Current + amount, 0, target), UpdatedAt = now };

    public bool IsComplete(int target) => Current >= target;
}

public sealed record Resource(AltId AltId, string Key, int Current, int Max, DateTimeOffset UpdatedAt)
{
    public Resource Spend(int amount, DateTimeOffset now) =>
        this with { Current = Math.Max(0, Current - amount), UpdatedAt = now };

    public Resource Gain(int amount, DateTimeOffset now) =>
        this with { Current = Math.Min(Max, Current + amount), UpdatedAt = now };
}

public abstract record ProgressEvent(Guid Id, DateTimeOffset At, AltId AltId);
public sealed record TaskProgressed(Guid Id, DateTimeOffset At, AltId AltId, TaskId TaskId, int Current) : ProgressEvent(Id, At, AltId);
public sealed record TaskCompleted(Guid Id, DateTimeOffset At, AltId AltId, TaskId TaskId) : ProgressEvent(Id, At, AltId);
public sealed record ResourceChanged(Guid Id, DateTimeOffset At, AltId AltId, string Key, int Current) : ProgressEvent(Id, At, AltId);
