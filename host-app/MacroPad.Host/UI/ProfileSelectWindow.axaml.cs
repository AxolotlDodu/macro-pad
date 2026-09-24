using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MacroPad.Host;

public partial class ProfileSelectWindow : Window
{
    private RadioButton _tenKey = null!, _twelveKey = null!;
    public PadProfile Result { get; private set; } = PadProfile.TenKeyScreen;

    public ProfileSelectWindow(PadProfile current)
    {
        AvaloniaXamlLoader.Load(this);
        _tenKey = this.FindControl<RadioButton>("TenKeyOption")!;
        _twelveKey = this.FindControl<RadioButton>("TwelveKeyOption")!;

        _tenKey.IsChecked = current == PadProfile.TenKeyScreen;
        _twelveKey.IsChecked = current == PadProfile.TwelveKeyNoScreen;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Result = _twelveKey.IsChecked == true ? PadProfile.TwelveKeyNoScreen : PadProfile.TenKeyScreen;
        Close();
    }
}