using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MacroPad.Host;

public partial class ConfigWindow : Window
{
    private readonly string _configPath;
    public ObservableCollection<BindingRowViewModel> Rows { get; } = new();

    public ConfigWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

        var config = File.Exists(_configPath)
            ? JsonSerializer.Deserialize<Config>(File.ReadAllText(_configPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Config()
            : new Config();

        for (int bit = 0; bit < 12; bit++)
        {
            config.Bindings.TryGetValue(bit.ToString(), out var existing);
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
        var config = new Config();
        foreach (var row in Rows)
        {
            if (row.Type == "none" || string.IsNullOrWhiteSpace(row.Target)) continue;
            config.Bindings[row.Bit.ToString()] = new BindingConfig
            {
                Type = row.Type,
                Target = row.Target,
                Method = row.Method
            };
        }

        File.WriteAllText(_configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        Close();
    }
}