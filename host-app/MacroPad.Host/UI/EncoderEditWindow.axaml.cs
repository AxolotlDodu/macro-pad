using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MacroPad.Host.Integrations;

namespace MacroPad.Host;

public partial class EncoderEditWindow : Window
{
    private readonly IReadOnlyList<string> _pageNames;
    private readonly IReadOnlyList<ActionTypeDescriptor> _clickDescriptors;
    private readonly IReadOnlyList<ActionTypeDescriptor> _rotationDescriptors;
    private readonly List<ActionMenuItem> _clickTopLevel;
    private readonly List<ActionMenuItem> _rotationTopLevel;

    private TextBlock _headerText = null!;
    private ComboBox _clickIntegrationBox = null!, _clickTypeBox = null!, _clickTargetComboBox = null!, _clickMethodBox = null!;
    private ComboBox _rotationIntegrationBox = null!, _rotationTypeBox = null!, _rotationTargetComboBox = null!;
    private StackPanel _clickActionPanel = null!, _clickTargetTextPanel = null!, _clickTargetComboPanel = null!, _clickMethodPanel = null!;
    private StackPanel _rotationActionPanel = null!, _rotationTargetComboPanel = null!;
    private TextBlock _clickTargetTextLabel = null!;
    private TextBox _clickTargetTextBox = null!, _rotationStepBox = null!;

    public bool Confirmed { get; private set; }

    public string ClickType { get; private set; } = "none";
    public string ClickTarget { get; private set; } = "";
    public string ClickMethod { get; private set; } = "GET";
    public List<string> ClickExcludedDevices { get; private set; } = new();

    public string RotationType { get; private set; } = "none";
    public string RotationTarget { get; private set; } = "";
    public double RotationStep { get; private set; } = 0.05;

    public EncoderEditWindow(
        string label,
        string currentClickType, string currentClickTarget, string currentClickMethod, IReadOnlyList<string> currentClickExcludedDevices,
        string currentRotationType, string currentRotationTarget, double currentRotationStep,
        IReadOnlyList<string> pageNames,
        IReadOnlyList<ActionTypeDescriptor> clickActionTypes,
        IReadOnlyList<ActionTypeDescriptor> rotationActionTypes)
    {
        _pageNames = pageNames;
        _clickDescriptors = clickActionTypes;
        _rotationDescriptors = rotationActionTypes;
        _clickTopLevel = ActionMenuBuilder.BuildTopLevel(_clickDescriptors);
        _rotationTopLevel = ActionMenuBuilder.BuildTopLevel(_rotationDescriptors);

        AvaloniaXamlLoader.Load(this);

        _headerText = this.FindControl<TextBlock>("HeaderText")!;
        _clickIntegrationBox = this.FindControl<ComboBox>("ClickIntegrationBox")!;
        _clickTypeBox = this.FindControl<ComboBox>("ClickTypeBox")!;
        _clickActionPanel = this.FindControl<StackPanel>("ClickActionPanel")!;
        _clickTargetTextPanel = this.FindControl<StackPanel>("ClickTargetTextPanel")!;
        _clickTargetTextLabel = this.FindControl<TextBlock>("ClickTargetTextLabel")!;
        _clickTargetComboPanel = this.FindControl<StackPanel>("ClickTargetComboPanel")!;
        _clickMethodPanel = this.FindControl<StackPanel>("ClickMethodPanel")!;
        _clickTargetTextBox = this.FindControl<TextBox>("ClickTargetTextBox")!;
        _clickTargetComboBox = this.FindControl<ComboBox>("ClickTargetComboBox")!;
        _clickMethodBox = this.FindControl<ComboBox>("ClickMethodBox")!;

        _rotationIntegrationBox = this.FindControl<ComboBox>("RotationIntegrationBox")!;
        _rotationTypeBox = this.FindControl<ComboBox>("RotationTypeBox")!;
        _rotationActionPanel = this.FindControl<StackPanel>("RotationActionPanel")!;
        _rotationTargetComboPanel = this.FindControl<StackPanel>("RotationTargetComboPanel")!;
        _rotationTargetComboBox = this.FindControl<ComboBox>("RotationTargetComboBox")!;
        _rotationStepBox = this.FindControl<TextBox>("RotationStepBox")!;

        _headerText.Text = label;

        _clickIntegrationBox.ItemsSource = _clickTopLevel;
        _clickIntegrationBox.SelectedItem = ActionMenuBuilder.ResolveTopLevel(_clickTopLevel, _clickDescriptors, currentClickType) ?? _clickTopLevel[0];
        _clickMethodBox.SelectedItem = currentClickMethod;

        _rotationIntegrationBox.ItemsSource = _rotationTopLevel;
        _rotationIntegrationBox.SelectedItem = ActionMenuBuilder.ResolveTopLevel(_rotationTopLevel, _rotationDescriptors, currentRotationType) ?? _rotationTopLevel[0];
        _rotationStepBox.Text = currentRotationStep.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        _clickIntegrationBox.SelectionChanged += (_, _) => UpdateClickGroupSelection(currentClickTarget, currentClickExcludedDevices);
        _clickTypeBox.SelectionChanged += (_, _) => UpdateClickFieldsVisibility(currentClickTarget, currentClickExcludedDevices);
        _rotationIntegrationBox.SelectionChanged += (_, _) => UpdateRotationGroupSelection(currentRotationTarget);
        _rotationTypeBox.SelectionChanged += (_, _) => UpdateRotationFieldsVisibility(currentRotationTarget);

        UpdateClickGroupSelection(currentClickTarget, currentClickExcludedDevices);
        UpdateRotationGroupSelection(currentRotationTarget);
    }

    private ActionTypeDescriptor? CurrentClickDescriptor()
    {
        if (_clickIntegrationBox.SelectedItem is not ActionMenuItem item) return null;
        if (item.DirectTypeId is not null) return ActionMenuBuilder.Find(_clickDescriptors, item.DirectTypeId);
        return _clickTypeBox.SelectedItem as ActionTypeDescriptor;
    }

    private ActionTypeDescriptor? CurrentRotationDescriptor()
    {
        if (_rotationIntegrationBox.SelectedItem is not ActionMenuItem item) return null;
        if (item.DirectTypeId is not null) return ActionMenuBuilder.Find(_rotationDescriptors, item.DirectTypeId);
        return _rotationTypeBox.SelectedItem as ActionTypeDescriptor;
    }

    private void UpdateClickGroupSelection(string currentTarget, IReadOnlyList<string> excludedDevices)
    {
        var item = _clickIntegrationBox.SelectedItem as ActionMenuItem;

        if (item?.IntegrationKey is { } key)
        {
            var group = ActionMenuBuilder.ForGroup(_clickDescriptors, key);
            _clickTypeBox.ItemTemplate = new FuncDataTemplate<ActionTypeDescriptor>((d, _) => new TextBlock { Text = d?.Label ?? "" });
            _clickTypeBox.ItemsSource = group;
            _clickTypeBox.SelectedItem = group.FirstOrDefault();
            _clickActionPanel.IsVisible = true;
        }
        else
        {
            _clickActionPanel.IsVisible = false;
        }

        UpdateClickFieldsVisibility(currentTarget, excludedDevices);
    }

    private void UpdateRotationGroupSelection(string currentTarget)
    {
        var item = _rotationIntegrationBox.SelectedItem as ActionMenuItem;

        if (item?.IntegrationKey is { } key)
        {
            var group = ActionMenuBuilder.ForGroup(_rotationDescriptors, key);
            _rotationTypeBox.ItemTemplate = new FuncDataTemplate<ActionTypeDescriptor>((d, _) => new TextBlock { Text = d?.Label ?? "" });
            _rotationTypeBox.ItemsSource = group;
            _rotationTypeBox.SelectedItem = group.FirstOrDefault();
            _rotationActionPanel.IsVisible = true;
        }
        else
        {
            _rotationActionPanel.IsVisible = false;
        }

        UpdateRotationFieldsVisibility(currentTarget);
    }

    private void UpdateClickFieldsVisibility(string currentTarget, IReadOnlyList<string> excludedDevices)
    {
        var descriptor = CurrentClickDescriptor();
        var target = descriptor?.Target ?? TargetKind.None;

        _clickMethodPanel.IsVisible = descriptor?.SupportsHttpMethod ?? false;

        if (target == TargetKind.PageCombo)
        {
            _clickTargetComboBox.ItemTemplate = null;
            _clickTargetComboBox.ItemsSource = _pageNames;
            _clickTargetComboBox.SelectedItem = _pageNames.Contains(currentTarget) ? currentTarget : _pageNames.FirstOrDefault();
            _clickTargetComboPanel.IsVisible = true;
            _clickTargetTextPanel.IsVisible = false;
        }
        else if (target == TargetKind.ChannelCombo && descriptor?.ComboOptions is { } options)
        {
            _clickTargetComboBox.ItemTemplate = new FuncDataTemplate<string>(
                (value, _) => new TextBlock { Text = value is null ? "" : descriptor.ComboDisplayName?.Invoke(value) ?? value });
            _clickTargetComboBox.ItemsSource = options;
            _clickTargetComboBox.SelectedItem = options.Contains(currentTarget) ? currentTarget : options.FirstOrDefault();
            _clickTargetComboPanel.IsVisible = true;
            _clickTargetTextPanel.IsVisible = false;
        }
        else if (target == TargetKind.FreeText)
        {
            _clickTargetTextLabel.Text = "CIBLE";
            _clickTargetTextBox.Text = currentTarget;
            _clickTargetComboPanel.IsVisible = false;
            _clickTargetTextPanel.IsVisible = true;
        }
        else if (target == TargetKind.DeviceExcludeList)
        {
            _clickTargetTextLabel.Text = "PÉRIPHÉRIQUES EXCLUS (séparés par des virgules)";
            _clickTargetTextBox.Text = string.Join(", ", excludedDevices);
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
        var descriptor = CurrentRotationDescriptor();
        var target = descriptor?.Target ?? TargetKind.None;

        if (target == TargetKind.ChannelCombo && descriptor?.ComboOptions is { } options)
        {
            _rotationTargetComboBox.ItemTemplate = new FuncDataTemplate<string>(
                (value, _) => new TextBlock { Text = value is null ? "" : descriptor.ComboDisplayName?.Invoke(value) ?? value });
            _rotationTargetComboBox.ItemsSource = options;
            _rotationTargetComboBox.SelectedItem = options.Contains(currentTarget) ? currentTarget : options.FirstOrDefault();
            _rotationTargetComboPanel.IsVisible = true;
        }
        else
        {
            _rotationTargetComboPanel.IsVisible = false;
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var clickDescriptor = CurrentClickDescriptor();
        ClickType = clickDescriptor?.Id ?? "none";
        var isClickExcludeList = clickDescriptor?.Target == TargetKind.DeviceExcludeList;

        ClickTarget = _clickTargetComboPanel.IsVisible
            ? (_clickTargetComboBox.SelectedItem as string ?? "")
            : (_clickTargetTextPanel.IsVisible && !isClickExcludeList ? (_clickTargetTextBox.Text ?? "") : "");

        ClickExcludedDevices = isClickExcludeList
            ? (_clickTargetTextBox.Text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : new List<string>();

        ClickMethod = _clickMethodBox.SelectedItem as string ?? "GET";

        var rotationDescriptor = CurrentRotationDescriptor();
        RotationType = rotationDescriptor?.Id ?? "none";
        RotationTarget = _rotationTargetComboPanel.IsVisible ? (_rotationTargetComboBox.SelectedItem as string ?? "") : "";

        if (!double.TryParse(_rotationStepBox.Text?.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var step) || step <= 0)
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