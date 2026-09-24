using Avalonia.Controls;
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

    private TextBlock _headerText = null!;
    private ComboBox _typeBox = null!;
    private StackPanel _targetTextPanel = null!, _targetComboPanel = null!, _methodPanel = null!, _stepPanel = null!;
    private TextBox _targetTextBox = null!, _stepBox = null!;
    private ComboBox _targetComboBox = null!, _methodBox = null!;

    public bool Confirmed { get; private set; }
    public string ResultType { get; private set; } = "none";
    public string ResultTarget { get; private set; } = "";
    public string ResultMethod { get; private set; } = "GET";
    public double ResultStep { get; private set; } = 0.05;

    public BindingEditWindow(BindingEditMode mode, string label, string currentType, string currentTarget,
        string currentMethod, double currentStep, IReadOnlyList<string> pageNames,
        IReadOnlyList<ActionTypeDescriptor> actionTypes)
    {
        _mode = mode;
        _pageNames = pageNames;
        _descriptors = actionTypes;

        AvaloniaXamlLoader.Load(this);

        _headerText = this.FindControl<TextBlock>("HeaderText")!;
        _typeBox = this.FindControl<ComboBox>("TypeBox")!;
        _targetTextPanel = this.FindControl<StackPanel>("TargetTextPanel")!;
        _targetComboPanel = this.FindControl<StackPanel>("TargetComboPanel")!;
        _methodPanel = this.FindControl<StackPanel>("MethodPanel")!;
        _stepPanel = this.FindControl<StackPanel>("StepPanel")!;
        _targetTextBox = this.FindControl<TextBox>("TargetTextBox")!;
        _targetComboBox = this.FindControl<ComboBox>("TargetComboBox")!;
        _methodBox = this.FindControl<ComboBox>("MethodBox")!;
        _stepBox = this.FindControl<TextBox>("StepBox")!;

        _headerText.Text = label;

        var typeIds = new List<string> { "none" };
        typeIds.AddRange(_descriptors.Select(d => d.Id));
        _typeBox.ItemsSource = typeIds;
        _typeBox.SelectedItem = currentType;
        if (_typeBox.SelectedItem is null) _typeBox.SelectedIndex = 0;

        _targetTextBox.Text = currentTarget;
        _methodBox.SelectedItem = currentMethod;
        _stepBox.Text = currentStep.ToString("0.###");

        _typeBox.SelectionChanged += (_, _) => UpdateFieldsVisibility(currentTarget);
        UpdateFieldsVisibility(currentTarget);
    }

    private ActionTypeDescriptor? FindDescriptor(string type) => _descriptors.FirstOrDefault(d => d.Id == type);

    private void UpdateFieldsVisibility(string currentTarget)
    {
        var type = _typeBox.SelectedItem as string ?? "none";
        var descriptor = FindDescriptor(type);
        var target = descriptor?.Target ?? TargetKind.None;

        _stepPanel.IsVisible = _mode == BindingEditMode.Encoder;
        _methodPanel.IsVisible = descriptor?.SupportsHttpMethod ?? false;

        if (target == TargetKind.PageCombo)
        {
            _targetComboBox.ItemTemplate = null;
            _targetComboBox.ItemsSource = _pageNames;
            _targetComboBox.SelectedItem = _pageNames.Contains(currentTarget)
                ? currentTarget
                : _pageNames.FirstOrDefault();

            _targetComboPanel.IsVisible = true;
            _targetTextPanel.IsVisible = false;
        }
        else if (target == TargetKind.ChannelCombo && descriptor?.ComboOptions is { } options)
        {
            _targetComboBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>(
                (value, _) => new TextBlock { Text = descriptor.ComboDisplayName?.Invoke(value) ?? value });
            _targetComboBox.ItemsSource = options;
            _targetComboBox.SelectedItem = options.Contains(currentTarget)
                ? currentTarget
                : options.FirstOrDefault();

            _targetComboPanel.IsVisible = true;
            _targetTextPanel.IsVisible = false;
        }
        else if (target == TargetKind.FreeText)
        {
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
        ResultType = _typeBox.SelectedItem as string ?? "none";

        ResultTarget = _targetComboPanel.IsVisible
            ? (_targetComboBox.SelectedItem as string ?? "")
            : (_targetTextPanel.IsVisible ? (_targetTextBox.Text ?? "") : "");

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