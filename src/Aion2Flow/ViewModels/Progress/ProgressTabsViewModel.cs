using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cloris.Aion2Flow.ViewModels.Progress;

public sealed record ProgressItemViewModel(string Title, string Value);

public sealed class ProgressTabViewModel(string header, IReadOnlyList<ProgressItemViewModel> items)
{
    public string Header { get; } = header;
    public IReadOnlyList<ProgressItemViewModel> Items { get; } = items;
}

/// <summary>Casca das abas Overlay/Hoje/Semana/Alts/Timers com dados fake (sem banco).</summary>
public sealed partial class ProgressTabsViewModel : ObservableObject
{
    public ProgressTabsViewModel()
    {
        Tabs =
        [
            new("Overlay", [new("DPS", "0"), new("Tempo", "00:00")]),
            new("Hoje", [new("Diária 1", "0/1"), new("Diária 2", "0/1")]),
            new("Semana", [new("Semanal 1", "0/3")]),
            new("Alts", [new("Personagem principal", "-")]),
            new("Timers", [new("Reset diário", "--:--:--")]),
        ];
        SelectedTab = Tabs[0];
    }

    public ObservableCollection<ProgressTabViewModel> Tabs { get; }

    [ObservableProperty]
    public partial ProgressTabViewModel SelectedTab { get; set; }
}
