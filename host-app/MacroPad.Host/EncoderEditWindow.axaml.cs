using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MacroPad.Host;

public partial class EncoderEditWindow : Window
{
    private static readonly string[] ButtonTypes =
    {
        "none", "launch", "url", "keystroke", "http",
        "sonar-toggle-mute", "sonar-set-output", "sonar-set-mic",
        "sonar-cycle-output", "sonar-cycle-mic",
        "discord-toggle-mute", "discord-toggle-deafen", "switch-page"
    };

    private static readonly string[] RotationTypes = { "none", "sonar-volume" };
    private static readonly string[] ChannelBasedClickTypes = { "sonar-toggle-mute" };

    private readonly IReadOnlyList<string> _pageNames;

    private TextBlock _headerText = null!;
    private ComboBox _clickTypeBox = null!, _clickTargetComboBox = null!, _clickMethodBox = null!;
    private ComboBox _rotationTypeBox = null!, _rotationTargetComboBox = null!;
    private StackPanel _clickTargetTextPanel = null!, _clickTargetComboPanel = null!, _clickMethodPanel = null!;
    private StackPanel _rotationTargetComboPanel = null!;
    private TextBox _clickTargetTextBox = null!, _rotationStepBox = null!;

    public bool Confirmed { get; private set; }

    public string ClickType { get; private set; } = "none";
    public string ClickTarget { get; private set; } = "";
    public string ClickMethod { get; private set; } = "GET";

    public string RotationType { get; private set; } = "none";
    public string RotationTarget { get; private set; } = "";
    public double RotationStep { get; private set; } = 0.05;

    public EncoderEditWindow(
        string label,
        string currentClickType, string currentClickTarget, string currentClickMethod,
        string currentRotationType, string currentRotationTarget, double currentRotationStep,
        IReadOnlyList<string> pageNames)
    {
        _pageNames = pageNames;

        AvaloniaXamlLoader.Load(this);

        _headerText = this.FindControl<TextBlock>("HeaderText")!;
        _clickTypeBox = this.FindControl<ComboBox>("ClickTypeBox")!;
        _clickTargetTextPanel = this.FindControl<StackPanel>("ClickTargetTextPanel")!;
        _clickTargetComboPanel = this.FindControl<StackPanel>("ClickTargetComboPanel")!;
        _clickMethodPanel = this.FindControl<StackPanel>("ClickMethodPanel")!;
        _clickTargetTextBox = this.FindControl<TextBox>("ClickTargetTextBox")!;
        _clickTargetComboBox = this.FindControl<ComboBox>("ClickTargetComboBox")!;
        _clickMethodBox = this.FindControl<ComboBox>("ClickMethodBox")!;

        _rotationTypeBox = this.FindControl<ComboBox>("RotationTypeBox")!;
        _rotationTargetComboPanel = this.FindControl<StackPanel>("RotationTargetComboPanel")!;
        _rotationTargetComboBox = this.FindControl<ComboBox>("RotationTargetComboBox")!;
        _rotationStepBox = this.FindControl<TextBox>("RotationStepBox")!;

        _headerText.Text = label;

        _clickTypeBox.ItemsSource = ButtonTypes;
        _clickTypeBox.SelectedItem = currentClickType;
        if (_clickTypeBox.SelectedItem is null) _clickTypeBox.SelectedIndex = 0;
        _clickTargetTextBox.Text = currentClickTarget;
        _clickMethodBox.SelectedItem = currentClickMethod;

        _rotationTypeBox.ItemsSource = RotationTypes;
        _rotationTypeBox.SelectedItem = currentRotationType;
        if (_rotationTypeBox.SelectedItem is null) _rotationTypeBox.SelectedIndex = 0;
        _rotationTargetComboBox.ItemsSource = SonarChannels.All;
        _rotationTargetComboBox.SelectedItem = SonarChannels.All.Contains(currentRotationTarget)
            ? currentRotationTarget
            : SonarChannels.All.FirstOrDefault();
        _rotationStepBox.Text = currentRotationStep.ToString("0.###");

        _clickTypeBox.SelectionChanged += (_, _) => UpdateClickFieldsVisibility(currentClickTarget);
        _rotationTypeBox.SelectionChanged += (_, _) => UpdateRotationFieldsVisibility();

        UpdateClickFieldsVisibility(currentClickTarget);
        UpdateRotationFieldsVisibility();
    }

    private void UpdateClickFieldsVisibility(string currentTarget)
    {
        var type = _clickTypeBox.SelectedItem as string ?? "none";

        _clickMethodPanel.IsVisible = type == "http";

        if (type == "switch-page")
        {
            _clickTargetComboBox.ItemsSource = _pageNames;
            _clickTargetComboBox.SelectedItem = _pageNames.Contains(currentTarget)
                ? currentTarget
                : _pageNames.FirstOrDefault();

            _clickTargetComboPanel.IsVisible = true;
            _clickTargetTextPanel.IsVisible = false;
        }
        else if (ChannelBasedClickTypes.Contains(type))
        {
            _clickTargetComboBox.ItemsSource = SonarChannels.All;
            _clickTargetComboBox.SelectedItem = SonarChannels.All.Contains(currentTarget)
                ? currentTarget
                : SonarChannels.All.FirstOrDefault();

            _clickTargetComboPanel.IsVisible = true;
            _clickTargetTextPanel.IsVisible = false;
        }
        else if (type is "none" or "discord-toggle-mute" or "discord-toggle-deafen"
                 or "sonar-cycle-output" or "sonar-cycle-mic")
        {
            _clickTargetComboPanel.IsVisible = false;
            _clickTargetTextPanel.IsVisible = false;
        }
        else
        {
            _clickTargetComboPanel.IsVisible = false;
            _clickTargetTextPanel.IsVisible = true;
        }
    }

    private void UpdateRotationFieldsVisibility()
    {
        var type = _rotationTypeBox.SelectedItem as string ?? "none";
        _rotationTargetComboPanel.IsVisible = type == "sonar-volume";
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        ClickType = _clickTypeBox.SelectedItem as string ?? "none";
        ClickTarget = _clickTargetComboPanel.IsVisible
            ? (_clickTargetComboBox.SelectedItem as string ?? "")
            : (_clickTargetTextPanel.IsVisible ? (_clickTargetTextBox.Text ?? "") : "");
        ClickMethod = _clickMethodBox.SelectedItem as string ?? "GET";

        RotationType = _rotationTypeBox.SelectedItem as string ?? "none";
        RotationTarget = _rotationTargetComboPanel.IsVisible
            ? (_rotationTargetComboBox.SelectedItem as string ?? "")
            : "";

        if (!double.TryParse(_rotationStepBox.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var step) || step <= 0)
            step = 0.05;
        RotationStep = step;

        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}