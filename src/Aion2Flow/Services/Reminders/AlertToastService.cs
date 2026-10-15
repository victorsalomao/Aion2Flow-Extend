using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cloris.Aion2Flow.Domain.Progress;

namespace Cloris.Aion2Flow.Services.Reminders;

/// <summary>
/// Notifier do app. Toast nativo do Windows exige COM/WinRT (inviavel em AOT sem trimming warnings),
/// entao usa uma janela Avalonia pequena no canto, sempre no topo, que some em 6 s.
/// Funciona com o painel de progresso fechado.
/// </summary>
public sealed class AlertToastService : IResinNotifier, IEventSoonNotifier
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(6);
    private readonly List<Window> _open = [];

    public ValueTask NotifyFullAsync(ResinFullNotification notification, CancellationToken ct = default)
    {
        Show("Resina cheia", "A resina de um personagem esta completa.");
        return ValueTask.CompletedTask;
    }

    public ValueTask NotifyEventSoonAsync(EventSoonNotification notification, CancellationToken ct = default)
    {
        Show(notification.Name, "Comeca em ate 5 minutos.");
        return ValueTask.CompletedTask;
    }

    private void Show(string title, string text) => Dispatcher.UIThread.Post(() =>
    {
        var window = new Window
        {
            Width = 280,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = true,
            WindowDecorations = WindowDecorations.None,
            Background = new SolidColorBrush(Color.Parse("#111920")),
            Content = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#526676")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = title, FontWeight = FontWeight.Bold, Foreground = Brushes.White },
                        new TextBlock { Text = text, Foreground = new SolidColorBrush(Color.Parse("#F1F6FB")), TextWrapping = TextWrapping.Wrap },
                    },
                },
            },
            WindowStartupLocation = WindowStartupLocation.Manual,
        };

        var slot = _open.Count;
        _open.Add(window);
        var area = window.Screens.Primary?.WorkingArea;
        if (area is { } a)
        {
            var scale = window.Screens.Primary!.Scaling;
            window.Position = new PixelPoint(
                a.Right - (int)((280 + 16) * scale),
                a.Bottom - (int)((90 + 16) * scale * (slot + 1)));
        }

        window.Closed += (_, _) => _open.Remove(window);
        window.Show();
        DispatcherTimer.RunOnce(window.Close, Lifetime);
    });
}
