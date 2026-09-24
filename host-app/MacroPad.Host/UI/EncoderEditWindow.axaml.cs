using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MacroPad.Host.Integrations;

namespace MacroPad.Host;

public partial class EncoderEditWindow : Window
{
    private readonly IReadOnlyList<string> _pageNames;
    private readonly IReadOnlyList<ActionTypeDescriptor> _clickDescriptors;
    private readonly IReadOnlyList<ActionTypeDescriptor> _rotationDescriptors;

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
        IReadOnlyList<string> pageNames,
        IReadOnlyList<ActionTypeDescriptor> clickActionTypes,
        IReadOnlyList<ActionTypeDescriptor> rotationActionTypes)
    {
        _pageNames = pageNames;
        _clickDescriptors = clickActionTypes;
        _rotationDescriptors = rotationActionTypes;

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

        var clickIds = new List<string> { "none" };
        clickIds.AddRange(_clickDescriptors.Select(d => d.Id));
        _clickTypeBox.ItemsSource = clickIds;
        _clickTypeBox.SelectedItem = currentClickType;
        if (_clickTypeBox.SelectedItem is null) _clickTypeBox.SelectedIndex = 0;
        _clickTargetTextBox.Text = currentClickTarget;
        _clickMethodBox.SelectedItem = currentClickMethod;

        var rotationIds = new List<string> { "none" };
        rotationIds.AddRange(_rotationDescriptors.Select(d => d.Id));
        _rotationTypeBox.ItemsSource = rotationIds;
        _rotationTypeBox.SelectedItem = currentRotationType;
        if (_rotationTypeBox.SelectedItem is null) _rotationTypeBox.SelectedIndex = 0;
        _rotationStepBox.Text = currentRotationStep.ToString("0.###");

        _clickTypeBox.SelectionChanged += (_, _) => UpdateClickFieldsVisibility(currentClickTarget);
        _rotationTypeBox.SelectionChanged += (_, _) => UpdateRotationFieldsVisibility(currentRotationTarget);

        UpdateClickFieldsVisibility(currentClickTarget);
        UpdateRotationFieldsVisibility(currentRotationTarget);
    }

    private void UpdateClickFieldsVisibility(string currentTarget)
    {
        var type = _clickTypeBox.SelectedItem as string ?? "none";
        var descriptor = _clickDescriptors.FirstOrDefault(d => d.Id == type);
        var target = descriptor?.Target ?? TargetKind.None;

        _clickMethodPanel.IsVisible = descriptor?.SupportsHttpMethod ?? false;

        if (target == TargetKind.PageCombo)
        {
            _clickTargetComboBox.ItemTemplate = null;
            _clickTargetComboBox.ItemsSource = _pageNames;
            _clickTargetComboBox.SelectedItem = _pageNames.Contains(currentTarget)
                ? currentTarget
                : _pageNames.FirstOrDefault();

            _clickTargetComboPanel.IsVisible = true;
            _clickTargetTextPanel.IsVisible = false;
        }
        else if (target == TargetKind.ChannelCombo && descriptor?.ComboOptions is { } options)
        {
            _clickTargetComboBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>(
                (value, _) => new TextBlock { Text = descriptor.ComboDisplayName?.Invoke(value) ?? value });
            _clickTargetComboBox.ItemsSource = options;
            _clickTargetComboBox.SelectedItem = options.Contains(currentTarget)
                ? currentTarget
                : options.FirstOrDefault();

            _clickTargetComboPanel.IsVisible = true;
            _clickTargetTextPanel.IsVisible = false;
        }
        else if (target == TargetKind.FreeText)
        {
            _clickTargetComboPanel.IsVisible = false;
            _clickTargetTextPanel.IsVisible = true;
        }
        else
        {
            _clickTargetComboPanel.IsVisible = false;
            _clickTargetTextPanel.IsVisible = false;
        }
    }

    private void UpdateRotationFieldsVisibility(string currentTarget)
    {
        var type = _rotationTypeBox.SelectedItem as string ?? "none";
        var descriptor = _rotationDescriptors.FirstOrDefault(d => d.Id == type);
        var target = descriptor?.Target ?? TargetKind.None;

        if (target == TargetKind.ChannelCombo && descriptor?.ComboOptions is { } options)
        {
            _rotationTargetComboBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>(
                (value, _) => new TextBlock { Text = descriptor.ComboDisplayName?.Invoke(value) ?? value });
            _rotationTargetComboBox.ItemsSource = options;
            _rotationTargetComboBox.SelectedItem = options.Contains(currentTarget)
                ? currentTarget
                : options.FirstOrDefault();

            _rotationTargetComboPanel.IsVisible = true;
        }
        else
        {
            _rotationTargetComboPanel.IsVisible = false;
        }
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