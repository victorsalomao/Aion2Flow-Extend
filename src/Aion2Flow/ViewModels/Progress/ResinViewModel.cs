using System.Globalization;
using Cloris.Aion2Flow.Domain.Progress;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cloris.Aion2Flow.ViewModels.Progress;

/// <summary>Resina/energia: valor informado + taxa de recarga; calcula o valor atual e quando enche. Aviso in-app ao encher.</summary>
public sealed partial class ResinViewModel(IResourceRepository repository, IManualFieldRepository fields, ManualFieldService fieldService) : ObservableObject
{
    private const string ResinKey = ResinFields.Key;

    private Alt? _alt;
    private Resource? _resource;
    private RechargeRule _rule = RechargeRule.Default(ResinKey);
    private bool _wasFull;

    [ObservableProperty]
    public partial string CurrentInput { get; set; } = "0";

    [ObservableProperty]
    public partial string MaxInput { get; set; } = RechargeRule.DefaultMax.ToString(CultureInfo.CurrentCulture);

    [ObservableProperty]
    public partial string IntervalInput { get; set; } = RechargeRule.DefaultIntervalMinutes.ToString(CultureInfo.CurrentCulture);

    [ObservableProperty]
    public partial string PointsInput { get; set; } = RechargeRule.DefaultPoints.ToString(CultureInfo.CurrentCulture);

    [ObservableProperty]
    public partial bool IsSubscribed { get; set; }

    [ObservableProperty]
    public partial string ProjectedText { get; set; } = "-";

    [ObservableProperty]
    public partial string FullInText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notice { get; set; } = string.Empty;

    private bool _loading;

    partial void OnIsSubscribedChanged(bool value)
    {
        if (!_loading)
        {
            MaxInput = ResinFields.MaxFor(value).ToString(CultureInfo.CurrentCulture);
        }
    }

    public async Task LoadAsync(Alt? alt)
    {
        _alt = alt;
        _resource = null;
        Notice = string.Empty;
        if (alt is null)
        {
            Refresh();
            return;
        }

        _resource = (await repository.ListByAltAsync(alt.Id)).FirstOrDefault(r => r.Key == ResinKey);
        var list = await fields.ListByAltAsync(alt.Id);
        _rule = ResinFields.BuildRule(list);
        _loading = true;
        IsSubscribed = ResinFields.IsSubscribed(list);
        _loading = false;
        IntervalInput = ((long)_rule.Interval.TotalMinutes).ToString(CultureInfo.CurrentCulture);
        PointsInput = _rule.Points.ToString(CultureInfo.CurrentCulture);
        MaxInput = ResinFields.MaxFor(IsSubscribed).ToString(CultureInfo.CurrentCulture);
        if (_resource is not null)
        {
            var projected = ResinCalculator.Project(_resource, _rule, DateTimeOffset.UtcNow);
            CurrentInput = projected.ToString(CultureInfo.CurrentCulture);
            MaxInput = _resource.Max.ToString(CultureInfo.CurrentCulture);
            _wasFull = projected >= _resource.Max;
        }

        Refresh();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_alt is null
            || !int.TryParse(CurrentInput, NumberStyles.Integer, CultureInfo.CurrentCulture, out var current)
            || !int.TryParse(MaxInput, NumberStyles.Integer, CultureInfo.CurrentCulture, out var max)
            || !long.TryParse(IntervalInput, NumberStyles.Integer, CultureInfo.CurrentCulture, out var minutes)
            || !int.TryParse(PointsInput, NumberStyles.Integer, CultureInfo.CurrentCulture, out var points)
            || max <= 0 || minutes <= 0 || points <= 0)
        {
            return;
        }

        _rule = _rule with { Interval = TimeSpan.FromMinutes(minutes), Points = points };
        _resource = new Resource(_alt.Id, ResinKey, Math.Clamp(current, 0, max), max, DateTimeOffset.UtcNow);
        await repository.SaveAsync(_resource);
        await fieldService.SetAsync(_alt.Id, ResinFields.Interval, minutes);
        await fieldService.SetAsync(_alt.Id, ResinFields.Points, points);
        await fieldService.SetAsync(_alt.Id, ResinFields.Subscribed, IsSubscribed ? 1 : 0);
        Notice = string.Empty;
        _wasFull = _resource.Current >= _resource.Max;
        Refresh();
    }

    /// <summary>Timer de 1 s (so com o painel visivel).</summary>
    public void Tick() => Refresh();

    private void Refresh()
    {
        if (_resource is null)
        {
            ProjectedText = "-";
            FullInText = "Informe o valor atual e salve";
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var projected = ResinCalculator.Project(_resource, _rule, now);
        ProjectedText = $"{projected}/{_resource.Max}";
        var isFull = projected >= _resource.Max;
        FullInText = isFull ? "Cheia" : $"Cheia em {ChecklistViewModel.FormatSpan(ResinCalculator.FullAt(_resource, _rule) - now)}";
        if (isFull && !_wasFull)
        {
            Notice = "Resina cheia!";
        }

        _wasFull = isFull;
    }

    [RelayCommand]
    private void DismissNotice() => Notice = string.Empty;
}
