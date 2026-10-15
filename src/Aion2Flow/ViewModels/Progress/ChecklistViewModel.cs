using System.Collections.ObjectModel;
using System.Globalization;
using Cloris.Aion2Flow.Domain.Progress;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cloris.Aion2Flow.ViewModels.Progress;

public sealed partial class ChecklistRowViewModel(ChecklistItem item, Func<ChecklistRowViewModel, int, Task> setCount) : ObservableObject
{
    public string TaskId { get; } = item.Task.Id;
    public string Name { get; } = item.Task.Name;
    public int Max { get; } = item.Task.MaxCount;

    [ObservableProperty]
    public partial int Current { get; set; } = item.Current;

    [ObservableProperty]
    public partial bool IsDone { get; set; } = item.IsDone;

    public string Progress => $"{Current}/{Max}";

    partial void OnCurrentChanged(int value) => OnPropertyChanged(nameof(Progress));

    public void Apply(ChecklistItem updated)
    {
        Current = updated.Current;
        IsDone = updated.IsDone;
    }

    [RelayCommand]
    private Task IncrementAsync() => setCount(this, Current + 1 > Max ? 0 : Current + 1);

    [RelayCommand]
    private Task ToggleAsync() => setCount(this, IsDone ? 0 : Max);
}

/// <summary>Checklist de um tipo de cadencia (diaria ou semanal) para o alt selecionado.</summary>
public sealed partial class ChecklistViewModel(IChecklistService service, IResetClock clock, TaskCadence cadence) : ObservableObject
{
    private Alt? _alt;

    public ObservableCollection<ChecklistRowViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial string NextResetText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    public async Task LoadAsync(Alt? alt)
    {
        _alt = alt;
        Items.Clear();
        if (alt is null)
        {
            Summary = "Selecione um alt";
            return;
        }

        foreach (var item in await service.GetAsync(alt.Id))
        {
            if (item.Task.Cadence == cadence)
            {
                Items.Add(new ChecklistRowViewModel(item, SetCountAsync));
            }
        }

        UpdateTexts();
    }

    /// <summary>Chamado pelo timer de 1 s enquanto o painel esta visivel.</summary>
    public void Tick() => UpdateTexts();

    private async Task SetCountAsync(ChecklistRowViewModel row, int count)
    {
        if (_alt is null)
        {
            return;
        }

        if (await service.SetCountAsync(_alt.Id, row.TaskId, count) is { } updated)
        {
            row.Apply(updated);
            UpdateTexts();
        }
    }

    private void UpdateTexts()
    {
        var next = cadence == TaskCadence.Weekly ? clock.NextWeekly() : clock.NextDaily();
        var left = next - DateTimeOffset.UtcNow;
        NextResetText = string.Create(CultureInfo.CurrentCulture,
            $"Próximo reset: {next.ToLocalTime():dd/MM HH:mm} (em {FormatSpan(left)})");
        Summary = $"{Items.Count(i => i.IsDone)}/{Items.Count} concluídas";
    }

    internal static string FormatSpan(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        return span.TotalDays >= 1
            ? string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalDays}d {span.Hours:00}h {span.Minutes:00}m")
            : string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}");
    }
}
