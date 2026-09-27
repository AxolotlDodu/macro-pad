using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MacroPad.Host.Integrations;

namespace MacroPad.Host;

public enum BindingEditMode { Button, Encoder }

public partial class BindingEditWindow : Window
{
    private readonly BindingEditMode _mode;
    private readonly IReadOnlyList<string> _pageNames;
    private readonly IReadOnlyList<ActionTypeDescriptor> _descriptors;
    private readonly List<ActionMenuItem> _topLevelItems;

    private TextBlock _headerText = null!;
    private ComboBox _integrationBox = null!, _typeBox = null!;
    private StackPanel _actionPanel = null!;
    private StackPanel _targetTextPanel = null!, _targetComboPanel = null!, _methodPanel = null!, _stepPanel = null!;
    private TextBlock _targetTextLabel = null!;
    private TextBox _targetTextBox = null!, _stepBox = null!;
    private ComboBox _targetComboBox = null!, _methodBox = null!;

    public bool Confirmed { get; private set; }
    public string ResultType { get; private set; } = "none";
    public string ResultTarget { get; private set; } = "";
    public string ResultMethod { get; private set; } = "GET";
    public double ResultStep { get; private set; } = 0.05;
    public List<string> ResultExcludedDevices { get; private set; } = new();

    public BindingEditWindow(BindingEditMode mode, string label, string currentType, string currentTarget,
        string currentMethod, double currentStep, IReadOnlyList<string> excludedDevices, IReadOnlyList<string> pageNames,
        IReadOnlyList<ActionTypeDescriptor> actionTypes)
    {
        _mode = mode;
        _pageNames = pageNames;
        _descriptors = actionTypes;
        _topLevelItems = ActionMenuBuilder.BuildTopLevel(_descriptors);

        AvaloniaXamlLoader.Load(this);

        _headerText = this.FindControl<TextBlock>("HeaderText")!;
        _integrationBox = this.FindControl<ComboBox>("IntegrationBox")!;
        _typeBox = this.FindControl<ComboBox>("TypeBox")!;
        _actionPanel = this.FindControl<StackPanel>("ActionPanel")!;
        _targetTextPanel = this.FindControl<StackPanel>("TargetTextPanel")!;
        _targetTextLabel = this.FindControl<TextBlock>("TargetTextLabel")!;
        _targetComboPanel = this.FindControl<StackPanel>("TargetComboPanel")!;
        _methodPanel = this.FindControl<StackPanel>("MethodPanel")!;
        _stepPanel = this.FindControl<StackPanel>("StepPanel")!;
        _targetTextBox = this.FindControl<TextBox>("TargetTextBox")!;
        _targetComboBox = this.FindControl<ComboBox>("TargetComboBox")!;
        _methodBox = this.FindControl<ComboBox>("MethodBox")!;
        _stepBox = this.FindControl<TextBox>("StepBox")!;

        _headerText.Text = label;

        _integrationBox.ItemsSource = _topLevelItems;
        _integrationBox.SelectedItem = ActionMenuBuilder.ResolveTopLevel(_topLevelItems, _descriptors, currentType) ?? _topLevelItems[0];

        _methodBox.SelectedItem = currentMethod;
        _stepBox.Text = currentStep.ToString("0.###");

        _integrationBox.SelectionChanged += (_, _) => UpdateGroupSelection(currentType, currentTarget, excludedDevices);
        _typeBox.SelectionChanged += (_, _) => UpdateFieldsVisibility(currentTarget, excludedDevices);

        UpdateGroupSelection(currentType, currentTarget, excludedDevices);
    }

    private ActionTypeDescriptor? CurrentDescriptor()
    {
        if (_integrationBox.SelectedItem is not ActionMenuItem item) return null;
        if (item.DirectTypeId is not null) return ActionMenuBuilder.Find(_descriptors, item.DirectTypeId);
        return _typeBox.SelectedItem as ActionTypeDescriptor;
    }

    private void UpdateGroupSelection(string currentType, string currentTarget, IReadOnlyList<string> excludedDevices)
    {
        var item = _integrationBox.SelectedItem as ActionMenuItem;

        if (item?.IntegrationKey is { } key)
        {
            var group = ActionMenuBuilder.ForGroup(_descriptors, key);
            _typeBox.ItemTemplate = new FuncDataTemplate<ActionTypeDescriptor>((d, _) => new TextBlock { Text = d?.Label ?? "" });
            _typeBox.ItemsSource = group;
            _typeBox.SelectedItem = group.FirstOrDefault(d => d.Id == currentType) ?? group.FirstOrDefault();
            _actionPanel.IsVisible = true;
        }
        else
        {
            _actionPanel.IsVisible = false;
        }

        UpdateFieldsVisibility(currentTarget, excludedDevices);
    }

    private void UpdateFieldsVisibility(string currentTarget, IReadOnlyList<string> excludedDevices)
    {
        var descriptor = CurrentDescriptor();
        var target = descriptor?.Target ?? TargetKind.None;

        _stepPanel.IsVisible = _mode == BindingEditMode.Encoder;
        _methodPanel.IsVisible = descriptor?.SupportsHttpMethod ?? false;

        if (target == TargetKind.PageCombo)
        {
            _targetComboBox.ItemTemplate = null;
            _targetComboBox.ItemsSource = _pageNames;
            _targetComboBox.SelectedItem = _pageNames.Contains(currentTarget) ? currentTarget : _pageNames.FirstOrDefault();
            _targetComboPanel.IsVisible = true;
            _targetTextPanel.IsVisible = false;
        }
        else if (target == TargetKind.ChannelCombo && descriptor?.ComboOptions is { } options)
        {
            _targetComboBox.ItemTemplate = new FuncDataTemplate<string>(
                (value, _) => new TextBlock { Text = value is null ? "" : (descriptor.ComboDisplayName?.Invoke(value) ?? value) });
            _targetComboBox.ItemsSource = options;
            _targetComboBox.SelectedItem = options.Contains(currentTarget) ? currentTarget : options.FirstOrDefault();
            _targetComboPanel.IsVisible = true;
            _targetTextPanel.IsVisible = false;
        }
        else if (target == TargetKind.FreeText)
        {
            _targetTextLabel.Text = "CIBLE";
            _targetTextBox.Text = currentTarget;
            _targetComboPanel.IsVisible = false;
            _targetTextPanel.IsVisible = true;
        }
        else if (target == TargetKind.DeviceExcludeList)
        {
            _targetTextLabel.Text = "PÉRIPHÉRIQUES EXCLUS (séparés par des virgules)";
            _targetTextBox.Text = string.Join(", ", excludedDevices);
            _targetComboPanel.IsVisible = false;
            _targetTextPanel.IsVisible = true;
        }
        else
        {
            _targetComboPanel.IsVisible = false;
            _targetTextPanel.IsVisible = false;
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var descriptor = CurrentDescriptor();
        ResultType = descriptor?.Id ?? "none";

        var isExcludeList = descriptor?.Target == TargetKind.DeviceExcludeList;

        ResultTarget = _targetComboPanel.IsVisible
            ? (_targetComboBox.SelectedItem as string ?? "")
            : (_targetTextPanel.IsVisible && !isExcludeList ? (_targetTextBox.Text ?? "") : "");

        ResultExcludedDevices = isExcludeList
            ? (_targetTextBox.Text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : new List<string>();

        ResultMethod = _methodBox.SelectedItem as string ?? "GET";

        if (!double.TryParse(_stepBox.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var step) || step <= 0)
            step = 0.05;
        ResultStep = step;

        Confirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}