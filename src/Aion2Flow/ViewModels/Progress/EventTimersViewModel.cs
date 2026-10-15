using System.Collections.ObjectModel;
using Cloris.Aion2Flow.Domain.Progress;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cloris.Aion2Flow.ViewModels.Progress;

public sealed partial class EventTimerRowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Countdown { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSoon { get; set; }
}

/// <summary>Countdown dos eventos; aviso in-app quando faltam ~5 min para comecar.</summary>
public sealed partial class EventTimersViewModel(IEventTimerService service) : ObservableObject
{
    private static readonly TimeSpan WarnThreshold = TimeSpan.FromMinutes(5);

    public ObservableCollection<EventTimerRowViewModel> Rows { get; } = [];

    [ObservableProperty]
    public partial string Warning { get; set; } = string.Empty;

    public void Tick()
    {
        var all = service.GetAll();
        while (Rows.Count < all.Count)
        {
            Rows.Add(new EventTimerRowViewModel());
        }

        while (Rows.Count > all.Count)
        {
            Rows.RemoveAt(Rows.Count - 1);
        }

        string? warning = null;
        for (var i = 0; i < all.Count; i++)
        {
            var t = all[i];
            var row = Rows[i];
            var soon = !t.IsActive && t.Remaining <= WarnThreshold;
            row.Name = t.Event.Name;
            row.Status = t.IsActive ? "Em andamento, termina em" : "Começa em";
            row.Countdown = ChecklistViewModel.FormatSpan(t.Remaining);
            row.IsSoon = soon;
            if (soon && warning is null)
            {
                warning = $"{t.Event.Name} começa em {ChecklistViewModel.FormatSpan(t.Remaining)}";
            }
        }

        Warning = warning ?? string.Empty;
    }
}
