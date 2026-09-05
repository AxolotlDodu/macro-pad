using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MacroPad.Host;

public partial class ConfigWindow : Window
{
    private readonly string _configPath;
    private readonly Config _config;

    private readonly ObservableCollection<PageConfig> _pages;
    private PageConfig? _currentPage;

    private ListBox _pageListBox = null!;
    private TextBox _pageNameBox = null!;
    private TextBox _appMatchersBox = null!;

    private readonly Dictionary<int, Button> _bitButtons = new();
    private Button _enc1Button = null!;
    private Button _enc2Button = null!;

    private readonly Dictionary<int, Button> _bitButtonsTwelveKey = new();
    private StackPanel _tenKeyLayout = null!;
    private Border _twelveKeyLayout = null!;
    private ComboBox _pageSwitchBox = null!;
    private static readonly Avalonia.Media.IBrush ReservedBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F9E2AF"));

    private static readonly Avalonia.Media.IBrush AssignedBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#89B4FA"));
    private static readonly Avalonia.Media.IBrush UnassignedPadBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#313244"));
    private static readonly Avalonia.Media.IBrush AssignedForeground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E2E"));
    private static readonly Avalonia.Media.IBrush UnassignedForeground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CDD6F4"));

    public ConfigWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        _config = Config.Load(_configPath);

        _pages = new ObservableCollection<PageConfig>(_config.Pages);

        _pageListBox = this.FindControl<ListBox>("PageList")!;
        _pageNameBox = this.FindControl<TextBox>("PageNameBox")!;
        _appMatchersBox = this.FindControl<TextBox>("AppMatchersBox")!;

        // bit -> numéro affiché sur la touche (bit 0 = touche 1, ..., bit 8 = touche 9)
        _bitButtons[0] = this.FindControl<Button>("Bit0")!;
        _bitButtons[1] = this.FindControl<Button>("Bit1")!;
        _bitButtons[2] = this.FindControl<Button>("Bit2")!;
        _bitButtons[3] = this.FindControl<Button>("Bit3")!;
        _bitButtons[4] = this.FindControl<Button>("Bit4")!;
        _bitButtons[5] = this.FindControl<Button>("Bit5")!;
        _bitButtons[6] = this.FindControl<Button>("Bit6")!;
        _bitButtons[7] = this.FindControl<Button>("Bit7")!;
        _bitButtons[8] = this.FindControl<Button>("Bit8")!;

        _enc1Button = this.FindControl<Button>("Enc1Button")!;
        _enc2Button = this.FindControl<Button>("Enc2Button")!;

        _tenKeyLayout = this.FindControl<StackPanel>("TenKeyLayout")!;
        _twelveKeyLayout = this.FindControl<Border>("TwelveKeyLayout")!;

        _bitButtonsTwelveKey[0]  = this.FindControl<Button>("Bit0b")!;
        _bitButtonsTwelveKey[1]  = this.FindControl<Button>("Bit1b")!;
        _bitButtonsTwelveKey[2]  = this.FindControl<Button>("Bit2b")!;
        _bitButtonsTwelveKey[3]  = this.FindControl<Button>("Bit3b")!;
        _bitButtonsTwelveKey[4]  = this.FindControl<Button>("Bit4b")!;
        _bitButtonsTwelveKey[5]  = this.FindControl<Button>("Bit5b")!;
        _bitButtonsTwelveKey[6]  = this.FindControl<Button>("Bit6b")!;
        _bitButtonsTwelveKey[7]  = this.FindControl<Button>("Bit7b")!;
        _bitButtonsTwelveKey[8]  = this.FindControl<Button>("Bit8b")!;
        _bitButtonsTwelveKey[9]  = this.FindControl<Button>("Bit9b")!;
        _bitButtonsTwelveKey[10] = this.FindControl<Button>("Bit10b")!;
        _bitButtonsTwelveKey[11] = this.FindControl<Button>("Bit11b")!;

        _pageSwitchBox = this.FindControl<ComboBox>("PageSwitchBox")!;
        _pageSwitchBox.ItemsSource = Enumerable.Range(1, 12).Select(n => n.ToString()).ToList();
        _pageSwitchBox.SelectedItem = (_config.PageSwitchBit + 1).ToString();
        _pageSwitchBox.SelectionChanged += (_, _) =>
        {
            if (_config.Profile != PadProfile.TwelveKeyNoScreen) return; // bit 9 fixe en 10 touches, non modifiable ici

            if (int.TryParse(_pageSwitchBox.SelectedItem as string, out var n))
                _config.PageSwitchBit = n - 1;
            if (_currentPage is not null) RefreshPadButtons(_currentPage);
        };

        _pageListBox.ItemsSource = _pages;
        _pageListBox.DisplayMemberBinding = new Binding("Name");

        ApplyProfileLayout();

        _pageListBox.SelectedIndex = 0;
    }

    private void OnPageSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is PageConfig previous)
            CommitCurrentPageEdits(previous);

        if (_pageListBox.SelectedItem is PageConfig page)
            LoadPage(page);
    }

    private void LoadPage(PageConfig page)
    {
        _currentPage = page;
        _pageNameBox.Text = page.Name;
        _appMatchersBox.Text = string.Join(", ", page.AppMatchers);
        RefreshPadButtons(page);
    }

    private void RefreshPadButtons(PageConfig page)
    {
        var activeButtons = _config.Profile == PadProfile.TenKeyScreen ? _bitButtons : _bitButtonsTwelveKey;

        foreach (var (bit, button) in activeButtons)
        {
            if (bit == _config.PageSwitchBit)
            {
                button.Content = $"{bit + 1}\nPAGE";
                button.Background = ReservedBrush;
                button.Foreground = AssignedForeground;
                button.FontWeight = Avalonia.Media.FontWeight.Bold;
                continue;
            }

            page.Bindings.TryGetValue(bit.ToString(), out var binding);
            bool assigned = binding is not null && binding.Type != "none";
            SetButtonState(button, (bit + 1).ToString(), assigned);
        }

        if (_config.Profile == PadProfile.TenKeyScreen)
        {
            page.Encoders.TryGetValue("1", out var enc1Rotation);
            page.Encoders.TryGetValue("2", out var enc2Rotation);
            page.Bindings.TryGetValue("10", out var enc1Click);
            page.Bindings.TryGetValue("11", out var enc2Click);

            bool enc1Assigned = (enc1Rotation is not null && enc1Rotation.Type != "none")
                            || (enc1Click is not null && enc1Click.Type != "none");
            bool enc2Assigned = (enc2Rotation is not null && enc2Rotation.Type != "none")
                            || (enc2Click is not null && enc2Click.Type != "none");

            SetButtonState(_enc1Button, "1", enc1Assigned);
            SetButtonState(_enc2Button, "2", enc2Assigned);
        }
    }

    private void ApplyProfileLayout()
    {
        bool isTenKey = _config.Profile == PadProfile.TenKeyScreen;
        _tenKeyLayout.IsVisible = isTenKey;
        _twelveKeyLayout.IsVisible = !isTenKey;

        if (_currentPage is not null) RefreshPadButtons(_currentPage);
    }

    private async void OnChangeProfileClick(object? sender, RoutedEventArgs e)
    {
        var dlg = new ProfileSelectWindow(_config.Profile);
        await dlg.ShowDialog(this);
        if (dlg.Result == _config.Profile) return;

        _config.Profile = dlg.Result;
        _config.PageSwitchBit = _config.Profile == PadProfile.TenKeyScreen ? 9 : 11;
        _config.Save(_configPath);

        _pageSwitchBox.SelectedItem = (_config.PageSwitchBit + 1).ToString(); // resynchronise l'affichage même si masqué

        ApplyProfileLayout();
        Console.WriteLine($"[ConfigWindow] Profil changé -> {_config.Profile}.");
    }

    private static void SetButtonState(Button button, string number, bool assigned)
    {
        button.Content = number;
        button.Background = assigned ? AssignedBrush : UnassignedPadBrush;
        button.Foreground = assigned ? AssignedForeground : UnassignedForeground;
        button.FontWeight = assigned ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
    }

    private void CommitCurrentPageEdits(PageConfig page)
    {
        page.Name = string.IsNullOrWhiteSpace(_pageNameBox.Text) ? page.Name : _pageNameBox.Text.Trim();
        page.AppMatchers = (_appMatchersBox.Text ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private async void OnBitButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_currentPage is null || sender is not Button btn) return;
        var bit = int.Parse((string)btn.Tag!);
        if (bit == _config.PageSwitchBit) return; // touche réservée, non éditable

        _currentPage.Bindings.TryGetValue(bit.ToString(), out var existing);

        var dlg = new BindingEditWindow(
            BindingEditMode.Button,
            $"Touche {bit + 1}",
            existing?.Type ?? "none",
            existing?.Target ?? "",
            existing?.Method ?? "GET",
            0,
            _pages.Select(p => p.Name).ToList());

        await dlg.ShowDialog(this);
        if (!dlg.Confirmed) return;

        if (dlg.ResultType == "none")
            _currentPage.Bindings.Remove(bit.ToString());
        else
            _currentPage.Bindings[bit.ToString()] = new BindingConfig
            {
                Type = dlg.ResultType,
                Target = dlg.ResultTarget,
                Method = dlg.ResultMethod
            };

        RefreshPadButtons(_currentPage);
    }

    private async void OnEncoderButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_currentPage is null || sender is not Button btn) return;
        var idx = (string)btn.Tag!; // "1" ou "2"
        var clickBit = idx == "1" ? "10" : "11";

        _currentPage.Bindings.TryGetValue(clickBit, out var existingClick);
        _currentPage.Encoders.TryGetValue(idx, out var existingRotation);

        var dlg = new EncoderEditWindow(
            $"Encodeur {idx}",
            existingClick?.Type ?? "none", existingClick?.Target ?? "", existingClick?.Method ?? "GET",
            existingRotation?.Type ?? "none", existingRotation?.Target ?? "", existingRotation?.Step ?? 0.05,
            _pages.Select(p => p.Name).ToList());

        await dlg.ShowDialog(this);
        if (!dlg.Confirmed) return;

        if (dlg.ClickType == "none")
            _currentPage.Bindings.Remove(clickBit);
        else
            _currentPage.Bindings[clickBit] = new BindingConfig
            {
                Type = dlg.ClickType,
                Target = dlg.ClickTarget,
                Method = dlg.ClickMethod
            };

        if (dlg.RotationType == "none")
            _currentPage.Encoders.Remove(idx);
        else
            _currentPage.Encoders[idx] = new EncoderConfig
            {
                Type = dlg.RotationType,
                Target = dlg.RotationTarget,
                Step = dlg.RotationStep
            };

        RefreshPadButtons(_currentPage);
    }

    private void OnAddPageClick(object? sender, RoutedEventArgs e)
    {
        int n = _pages.Count + 1;
        string name = $"Nouvelle page {n}";
        while (_pages.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            n++;
            name = $"Nouvelle page {n}";
        }

        var newPage = new PageConfig { Name = name };
        _pages.Add(newPage);
        _pageListBox.SelectedItem = newPage;
    }

    private void OnRemovePageClick(object? sender, RoutedEventArgs e)
    {
        if (_pages.Count <= 1)
        {
            Console.WriteLine("[ConfigWindow] Impossible de supprimer la dernière page.");
            return;
        }

        if (_pageListBox.SelectedItem is not PageConfig page) return;

        var idx = _pages.IndexOf(page);
        _currentPage = null;
        _pages.Remove(page);

        _pageListBox.SelectedIndex = Math.Max(0, idx - 1);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (_currentPage is not null)
            CommitCurrentPageEdits(_currentPage);

        _config.Pages = _pages.ToList();
        _config.Save(_configPath);

        Console.WriteLine("[ConfigWindow] Configuration enregistrée.");
    }
}