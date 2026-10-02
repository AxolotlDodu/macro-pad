using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace MacroPad.Host;

public partial class SettingsWindow : Window
{
    private static readonly string[] Positions =
        { "TopLeft", "TopCenter", "TopRight", "BottomLeft", "BottomCenter", "BottomRight" };
    private static readonly string[] PositionLabels =
        { "Haut gauche", "Haut centre", "Haut droite", "Bas gauche", "Bas centre", "Bas droite" };

    private readonly NotificationSettings _original;
    private PadProfile _profile;

    private TextBlock _profileText = null!, _previewText = null!;
    private ComboBox _positionBox = null!;
    private TextBox _widthBox = null!, _heightBox = null!, _marginBox = null!, _durationBox = null!;
    private TextBox _bgBox = null!, _fgBox = null!;
    private Border _previewBorder = null!;

    public bool Confirmed { get; private set; }
    public PadProfile ResultProfile { get; private set; }
    public NotificationSettings ResultNotification { get; private set; }

    public SettingsWindow(Config config)
    {
        AvaloniaXamlLoader.Load(this);

        _original = config.Notification;
        _profile = config.Profile;
        ResultProfile = _profile;
        ResultNotification = _original;

        _profileText = this.FindControl<TextBlock>("ProfileText")!;
        _positionBox = this.FindControl<ComboBox>("PositionBox")!;
        _widthBox = this.FindControl<TextBox>("WidthBox")!;
        _heightBox = this.FindControl<TextBox>("HeightBox")!;
        _marginBox = this.FindControl<TextBox>("MarginBox")!;
        _durationBox = this.FindControl<TextBox>("DurationBox")!;
        _bgBox = this.FindControl<TextBox>("BackgroundBox")!;
        _fgBox = this.FindControl<TextBox>("ForegroundBox")!;
        _previewBorder = this.FindControl<Border>("PreviewBorder")!;
        _previewText = this.FindControl<TextBlock>("PreviewText")!;

        _positionBox.ItemsSource = PositionLabels;
        var idx = Array.FindIndex(Positions, p => string.Equals(p, _original.Position, StringComparison.OrdinalIgnoreCase));
        _positionBox.SelectedIndex = idx >= 0 ? idx : 5;

        _widthBox.Text = _original.Width.ToString(CultureInfo.InvariantCulture);
        _heightBox.Text = _original.Height.ToString(CultureInfo.InvariantCulture);
        _marginBox.Text = _original.Margin.ToString(CultureInfo.InvariantCulture);
        _durationBox.Text = _original.DurationMs.ToString(CultureInfo.InvariantCulture);
        _bgBox.Text = _original.Background;
        _fgBox.Text = _original.Foreground;

        _bgBox.TextChanged += (_, _) => UpdatePreview();
        _fgBox.TextChanged += (_, _) => UpdatePreview();

        UpdateProfileText();
        UpdatePreview();
    }

    private void UpdateProfileText() => _profileText.Text = _profile == PadProfile.TenKeyScreen
        ? "MacroPad v2 : 10 touches + écran + 2 encodeurs"
        : "MacroPad v1 : 12 touches, sans écran";

    private void UpdatePreview()
    {
        if (Color.TryParse(_bgBox.Text?.Trim(), out var bg)) _previewBorder.Background = new SolidColorBrush(bg);
        if (Color.TryParse(_fgBox.Text?.Trim(), out var fg)) _previewText.Foreground = new SolidColorBrush(fg);
    }

    private async void OnChangeProfileClick(object? sender, RoutedEventArgs e)
    {
        var dlg = new ProfileSelectWindow(_profile);
        await dlg.ShowDialog(this);
        _profile = dlg.Result;
        UpdateProfileText();
    }

    private static int ParseInt(string? text, int fallback, int min, int max) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
            ? Math.Clamp(v, min, max) : fallback;

    private static string ParseColor(string? text, string fallback) =>
        Color.TryParse(text?.Trim(), out _) ? text!.Trim() : fallback;

    private NotificationSettings BuildSettings() => new()
    {
        Position = Positions[Math.Max(0, _positionBox.SelectedIndex)],
        Width = ParseInt(_widthBox.Text, _original.Width, 100, 1000),
        Height = ParseInt(_heightBox.Text, _original.Height, 30, 400),
        Margin = ParseInt(_marginBox.Text, _original.Margin, 0, 500),
        DurationMs = ParseInt(_durationBox.Text, _original.DurationMs, 300, 30000),
        Background = ParseColor(_bgBox.Text, _original.Background),
        Foreground = ParseColor(_fgBox.Text, _original.Foreground)
    };

    private void OnTestClick(object? sender, RoutedEventArgs e) =>
        OverlayNotifier.Show("Notification de test", BuildSettings());

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        ResultProfile = _profile;
        ResultNotification = BuildSettings();
        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}