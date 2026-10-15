using Cloris.Aion2Flow.Services.Logging;
using Cloris.Aion2Flow.Domain.Progress;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cloris.Aion2Flow.ViewModels.Progress;

/// <summary>Painel de progresso (Hoje/Semana/Alts/Recursos/Timers/Temporada). Criado ao abrir o flyout e descartado ao fechar.</summary>
public sealed class ProgressTabsViewModel : ObservableObject, IDisposable
{
    private bool _disposed;

    public ProgressTabsViewModel(
        IAltRepository altRepository,
        IChecklistService checklistService,
        IResetClock clock,
        IManualFieldRepository fieldRepository,
        ManualFieldService fieldService,
        IResourceRepository resourceRepository,
        IEventTimerService timerService)
    {
        Alts = new AltsViewModel(altRepository);
        Daily = new ChecklistViewModel(checklistService, clock, TaskCadence.Daily);
        Weekly = new ChecklistViewModel(checklistService, clock, TaskCadence.Weekly);
        Fields = new ManualFieldsViewModel(fieldRepository, fieldService, clock);
        Resin = new ResinViewModel(resourceRepository, fieldRepository, fieldService);
        Timers = new EventTimersViewModel(timerService);
        Season = new SeasonViewModel(fieldRepository, fieldService);
        Alts.SelectionChanged += OnAltSelected;
        Fields.Changed += OnFieldsChanged;
    }

    public AltsViewModel Alts { get; }
    public ChecklistViewModel Daily { get; }
    public ChecklistViewModel Weekly { get; }
    public ManualFieldsViewModel Fields { get; }
    public ResinViewModel Resin { get; }
    public EventTimersViewModel Timers { get; }
    public SeasonViewModel Season { get; }

    public async Task InitializeAsync()
    {
        Timers.Tick();
        await Alts.LoadAsync();
    }

    /// <summary>Chamado pelo DispatcherTimer de 1 s da view (ativo apenas com o painel visivel).</summary>
    public void Tick()
    {
        if (_disposed)
        {
            return;
        }

        Daily.Tick();
        Weekly.Tick();
        Resin.Tick();
        Timers.Tick();
    }

    public void Dispose()
    {
        _disposed = true;
        Alts.SelectionChanged -= OnAltSelected;
        Fields.Changed -= OnFieldsChanged;
    }

    private async void OnAltSelected(Alt? alt)
    {
        try
        {
            await Task.WhenAll(Daily.LoadAsync(alt), Weekly.LoadAsync(alt), Fields.LoadAsync(alt), Resin.LoadAsync(alt));
            await Season.LoadAsync(alt);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            AppLog.Write(AppLogLevel.Warning, $"[Progress] falha ao carregar alt: {ex.Message}");
        }
    }

    private async void OnFieldsChanged()
    {
        try
        {
            await Season.RefreshAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            AppLog.Write(AppLogLevel.Warning, $"[Progress] falha ao atualizar temporada: {ex.Message}");
        }
    }
}
