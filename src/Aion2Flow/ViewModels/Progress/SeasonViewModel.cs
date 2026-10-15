using System.Globalization;
using Cloris.Aion2Flow.Domain.Progress;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cloris.Aion2Flow.ViewModels.Progress;

/// <summary>Calculadora de temporada: meta, data final e pontos atuais (campo manual "season") para pontos por dia.</summary>
public sealed partial class SeasonViewModel(IManualFieldRepository repository, ManualFieldService service) : ObservableObject
{
    private const string TargetKey = "season.target";
    private const string EndKey = "season.end";

    private Alt? _alt;

    [ObservableProperty]
    public partial string TargetInput { get; set; } = "0";

    [ObservableProperty]
    public partial string EndInput { get; set; } = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [ObservableProperty]
    public partial string Result { get; set; } = string.Empty;

    public async Task LoadAsync(Alt? alt)
    {
        _alt = alt;
        if (alt is null)
        {
            Result = "Selecione um alt";
            return;
        }

        var saved = (await repository.ListByAltAsync(alt.Id)).ToDictionary(f => f.Key);
        if (saved.TryGetValue(TargetKey, out var t))
        {
            TargetInput = t.Value.ToString(CultureInfo.CurrentCulture);
        }

        if (saved.TryGetValue(EndKey, out var e)
            && DateTime.TryParseExact(e.Value.ToString(CultureInfo.InvariantCulture), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            EndInput = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        Compute(saved.TryGetValue(ManualFieldsViewModel.SeasonKey, out var p) ? p.Value : 0);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_alt is null
            || !long.TryParse(TargetInput, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var target)
            || !DateTime.TryParseExact(EndInput, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            Result = "Meta ou data inválida (use AAAA-MM-DD)";
            return;
        }

        await service.SetAsync(_alt.Id, TargetKey, target);
        await service.SetAsync(_alt.Id, EndKey, long.Parse(end.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
        var points = (await repository.ListByAltAsync(_alt.Id)).FirstOrDefault(f => f.Key == ManualFieldsViewModel.SeasonKey)?.Value ?? 0;
        Compute(points);
    }

    /// <summary>Recalcula quando os pontos de temporada mudam na aba de campos manuais.</summary>
    public async Task RefreshAsync()
    {
        if (_alt is not null)
        {
            await LoadAsync(_alt);
        }
    }

    private void Compute(long points)
    {
        if (!long.TryParse(TargetInput, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var target)
            || !DateTime.TryParseExact(EndInput, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            Result = string.Empty;
            return;
        }

        var p = SeasonCalculator.Compute(points, target, (end.Date - DateTime.Today).Days + 1);
        Result = p.Reached ? $"Meta atingida ({points:N0}/{target:N0})"
            : p.Ended ? $"Temporada encerrada; faltaram {p.Missing:N0} pontos"
            : $"Faltam {p.Missing:N0} pontos em {p.DaysLeft} dia(s): {p.PerDayNeeded:N0} por dia";
    }
}
