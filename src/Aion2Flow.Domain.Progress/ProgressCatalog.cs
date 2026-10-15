using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cloris.Aion2Flow.Domain.Progress;

public sealed record TaskDefinition(string Id, string Name, string Type, int MaxCount)
{
    public TaskCadence Cadence => string.Equals(Type, "weekly", StringComparison.OrdinalIgnoreCase)
        ? TaskCadence.Weekly : TaskCadence.Daily;

    public bool IsValid => !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Name) && MaxCount > 0
        && (string.Equals(Type, "daily", StringComparison.OrdinalIgnoreCase) || string.Equals(Type, "weekly", StringComparison.OrdinalIgnoreCase));
}

public sealed record TaskCatalogFile(List<TaskDefinition>? Tasks);

public sealed record EventDefinition(string Id, string Name, List<DayOfWeek>? Days, List<string>? Times, int DurationMinutes)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Name)
        && Days is { Count: > 0 } && Times is { Count: > 0 } && DurationMinutes > 0
        && Times.All(t => TimeSpan.TryParseExact(t, @"h\:mm", System.Globalization.CultureInfo.InvariantCulture, out _));
}

public sealed record EventCatalogFile(List<EventDefinition>? Events);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(TaskCatalogFile))]
[JsonSerializable(typeof(EventCatalogFile))]
public sealed partial class ProgressJsonContext : JsonSerializerContext;

public interface ITaskCatalog
{
    IReadOnlyList<TaskDefinition> Tasks { get; }
}

public interface IEventCatalog
{
    IReadOnlyList<EventDefinition> Events { get; }
}

public sealed class TaskCatalog(IReadOnlyList<TaskDefinition> tasks) : ITaskCatalog
{
    public IReadOnlyList<TaskDefinition> Tasks { get; } = tasks;

    public static TaskCatalog Parse(string json)
    {
        var file = JsonSerializer.Deserialize(json, ProgressJsonContext.Default.TaskCatalogFile);
        var list = (file?.Tasks ?? []).Where(t => t.IsValid).GroupBy(t => t.Id).Select(g => g.First()).ToList();
        return new TaskCatalog(list);
    }

    /// <summary>Arquivo ausente ou invalido resulta em catalogo vazio (nunca lanca).</summary>
    public static TaskCatalog Load(string path)
    {
        try { return File.Exists(path) ? Parse(File.ReadAllText(path)) : new TaskCatalog([]); }
        catch (JsonException) { return new TaskCatalog([]); }
        catch (IOException) { return new TaskCatalog([]); }
    }
}

public sealed class EventCatalog(IReadOnlyList<EventDefinition> events) : IEventCatalog
{
    public IReadOnlyList<EventDefinition> Events { get; } = events;

    public static EventCatalog Parse(string json)
    {
        var file = JsonSerializer.Deserialize(json, ProgressJsonContext.Default.EventCatalogFile);
        var list = (file?.Events ?? []).Where(e => e.IsValid).GroupBy(e => e.Id).Select(g => g.First()).ToList();
        return new EventCatalog(list);
    }

    public static EventCatalog Load(string path)
    {
        try { return File.Exists(path) ? Parse(File.ReadAllText(path)) : new EventCatalog([]); }
        catch (JsonException) { return new EventCatalog([]); }
        catch (IOException) { return new EventCatalog([]); }
    }
}
