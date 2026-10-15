using Avalonia.Controls;
using Avalonia.Threading;
using Cloris.Aion2Flow.ViewModels.Progress;

namespace Cloris.Aion2Flow.Views.Progress;

public partial class ProgressTabsView : UserControl
{
    private DispatcherTimer? _timer;

    public ProgressTabsView() => InitializeComponent();

    // Timer de 1 s existe somente enquanto a view esta anexada (flyout aberto).
    protected override async void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is not ProgressTabsViewModel vm)
        {
            return;
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => vm.Tick();
        _timer.Start();
        try
        {
            await vm.InitializeAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Cloris.Aion2Flow.Services.Logging.AppLog.Write(Cloris.Aion2Flow.Services.Logging.AppLogLevel.Warning, $"[Progress] falha ao iniciar painel: {ex.Message}");
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _timer?.Stop();
        _timer = null;
        (DataContext as IDisposable)?.Dispose();
        base.OnDetachedFromVisualTree(e);
    }
}
