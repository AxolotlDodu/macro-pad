using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace MacroPad.Host;

public static class OverlayNotifier
{
    private static NotificationOverlay? _window;

    public static void Show(string text, NotificationSettings settings) =>
        Dispatcher.UIThread.Post(() =>
        {
            _window ??= new NotificationOverlay();
            _window.Display(text, settings);
        });
}

public class NotificationOverlay : Window
{
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(IntPtr h, int i, int v);
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;

    private readonly TextBlock _text = new()
    {
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        FontSize = 18,
        FontWeight = FontWeight.SemiBold,
        TextTrimming = TextTrimming.CharacterEllipsis
    };
    private readonly Border _border = new() { CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 0) };
    private readonly DispatcherTimer _timer = new();

    public NotificationOverlay()
    {
        WindowDecorations = WindowDecorations.None;
        Background = Brushes.Transparent;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        CanResize = false;
        Focusable = false;

        _border.Child = _text;
        Content = _border;

        _timer.Tick += (_, _) => { _timer.Stop(); Hide(); };
    }

    public void Display(string text, NotificationSettings s)
    {
        var screen = Screens.Primary;
        if (screen is null) return;

        _text.Text = text;
        _text.Foreground = ParseBrush(s.Foreground, "#CDD6F4");
        _border.Background = ParseBrush(s.Background, "#1E1E2E");
        Width = s.Width;
        Height = s.Height;

        var area = screen.WorkingArea;
        double scale = screen.Scaling;
        int w = (int)(s.Width * scale), h = (int)(s.Height * scale), m = (int)(s.Margin * scale);

        int x = s.Position.EndsWith("Left", StringComparison.OrdinalIgnoreCase) ? area.X + m
              : s.Position.EndsWith("Right", StringComparison.OrdinalIgnoreCase) ? area.Right - w - m
              : area.X + (area.Width - w) / 2;
        int y = s.Position.StartsWith("Top", StringComparison.OrdinalIgnoreCase) ? area.Y + m
              : area.Bottom - h - m;

        Position = new PixelPoint(x, y);

        if (!IsVisible) Show();
        MakeClickThrough();

        _timer.Stop();
        _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(300, s.DurationMs));
        _timer.Start();
    }

    private void MakeClickThrough()
    {
        if (!OperatingSystem.IsWindows()) return;
        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero) return;
        SetWindowLong(handle, GWL_EXSTYLE,
            GetWindowLong(handle, GWL_EXSTYLE) | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    private static IBrush ParseBrush(string value, string fallback) =>
        new SolidColorBrush(Color.TryParse(value, out var c) ? c : Color.Parse(fallback));
}