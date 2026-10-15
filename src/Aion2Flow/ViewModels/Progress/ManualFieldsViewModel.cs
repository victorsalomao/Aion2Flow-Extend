using System.Collections.ObjectModel;
using System.Globalization;
using Cloris.Aion2Flow.Domain.Progress;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cloris.Aion2Flow.ViewModels.Progress;

public sealed partial class ManualFieldRowViewModel(string key, string label, Func<ManualFieldRowViewModel, Task> apply) : ObservableObject
{
    public string Key { get; } = key;
    public string Label { get; } = label;

    [ObservableProperty]
    public partial string InputText { get; set; } = "0";

    [ObservableProperty]
    public partial string DeltaText { get; set; } = "hoje: +0";

    [RelayCommand]
    private Task ApplyAsync() => apply(this);

    public void Show(long value, long delta)
    {
        InputText = value.ToString(CultureInfo.CurrentCulture);
        DeltaText = string.Create(CultureInfo.CurrentCulture, $"hoje: {delta:+#,0;-#,0;+0}");
    }
}

/// <summary>Campos numericos manuais por alt, com delta "hoje" desde o ultimo reset diario.</summary>
public sealed class ManualFieldsViewModel(IManualFieldRepository repository, ManualFieldService service, IResetClock clock)
{
    public const string GoldKey = "gold";
    public const string AbyssKey = "abyss";
    public const string SeasonKey = "season";

    private Alt? _alt;

    public ObservableCollection<ManualFieldRowViewModel> Rows { get; } = [];

    public event Action? Changed;

    public async Task LoadAsync(Alt? alt)
    {
        _alt = alt;
        Rows.Clear();
        if (alt is null)
        {
            return;
        }

        var saved = (await repository.ListByAltAsync(alt.Id)).ToDictionary(f => f.Key);
        var reset = clock.LastDaily();
        foreach (var (key, label) in new[] { (GoldKey, "Gold"), (AbyssKey, "Pontos de abismo"), (SeasonKey, "Pontos de temporada") })
        {
            var row = new ManualFieldRowViewModel(key, label, ApplyAsync);
            if (saved.TryGetValue(key, out var f))
            {
                row.Show(f.Value, f.TodayDelta(reset));
            }

            Rows.Add(row);
        }
    }

    private async Task ApplyAsync(ManualFieldRowViewModel row)
    {
        if (_alt is null || !long.TryParse(row.InputText, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var value))
        {
            return;
        }

        var f = await service.SetAsync(_alt.Id, row.Key, value);
        row.Show(f.Value, f.TodayDelta(clock.LastDaily()));
        Changed?.Invoke();
    }
}
