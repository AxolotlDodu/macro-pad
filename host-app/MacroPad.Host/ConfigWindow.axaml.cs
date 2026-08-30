using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MacroPad.Host;

public partial class ConfigWindow : Window
{
    private readonly string _configPath;
    private readonly Config _config;

    // NOTE: édite uniquement la 1ère page pour l'instant. Le vrai sélecteur de pages
    // (liste à gauche, add/remove/rename, AppMatchers) reste à construire côté XAML —
    // voir le message d'accompagnement pour le détail de ce qu'il reste à faire.
    private readonly PageConfig _page;

    public ObservableCollection<BindingRowViewModel> Rows { get; } = new();

    public ConfigWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        _config = Config.Load(_configPath);
        _page = _config.Pages[0];

        for (int bit = 0; bit < 12; bit++)
        {
            if (bit == 9)
            {
                // Réservé au changement de page (pin 14) — affiché mais non éditable/sauvegardé.
                Rows.Add(new BindingRowViewModel
                {
                    Bit = bit,
                    Type = "switch-page",
                    Target = "(réservé — page suivante)"
                });
                continue;
            }

            _page.Bindings.TryGetValue(bit.ToString(), out var existing);
            Rows.Add(new BindingRowViewModel
            {
                Bit = bit,
                Type = existing?.Type ?? "none",
                Target = existing?.Target ?? "",
                Method = existing?.Method ?? "GET"
            });
        }

        var list = this.FindControl<ItemsControl>("BindingsList")!;
        list.ItemsSource = Rows;
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        foreach (var row in Rows)
        {
            if (row.Bit == 9) continue; // verrouillé, jamais écrit depuis l'UI

            if (row.Type == "none" || string.IsNullOrWhiteSpace(row.Target))
            {
                _page.Bindings.Remove(row.Bit.ToString());
                continue;
            }

            _page.Bindings[row.Bit.ToString()] = new BindingConfig
            {
                Type = row.Type,
                Target = row.Target,
                Method = row.Method
            };
        }

        _config.Save(_configPath);
        Close();
    }
}
