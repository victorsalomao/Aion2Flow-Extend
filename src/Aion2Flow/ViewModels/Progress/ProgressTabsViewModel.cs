using System.Collections.ObjectModel;


namespace Cloris.Aion2Flow.ViewModels.Progress;

public sealed record ProgressItemViewModel(string Title, string Value);

public sealed class ProgressTabViewModel(string header, IReadOnlyList<ProgressItemViewModel> items)
{
    public string Header { get; } = header;
    public IReadOnlyList<ProgressItemViewModel> Items { get; } = items;
}

/// <summary>Casca das abas Overlay/Hoje/Semana/Alts/Timers com dados fake (sem banco).</summary>
public sealed class ProgressTabsViewModel : FrameBatchedObservableObject
{
    private ProgressTabViewModel _selectedTab;

    public ProgressTabsViewModel(UiFrameBatchService frameBatchService) : base(frameBatchService)
    {
        Tabs =
        [
            new("Overlay", [new("DPS", "0"), new("Tempo", "00:00")]),
            new("Hoje", [new("Diária 1", "0/1"), new("Diária 2", "0/1")]),
            new("Semana", [new("Semanal 1", "0/3")]),
            new("Alts", [new("Personagem principal", "-")]),
            new("Timers", [new("Reset diário", "--:--:--")]),
        ];
        _selectedTab = Tabs[0];
    }

    public ObservableCollection<ProgressTabViewModel> Tabs { get; }


    public ProgressTabViewModel SelectedTab
    {
        get => _selectedTab;
        set => SetFrameProperty(ref _selectedTab, value);
    }
}
