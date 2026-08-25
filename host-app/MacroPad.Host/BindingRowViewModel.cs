using System.ComponentModel;

namespace MacroPad.Host;

public class BindingRowViewModel : INotifyPropertyChanged
{
    public int Bit { get; set; }

    private string _type = "none";
    public string Type { get => _type; set { _type = value; OnPropertyChanged(nameof(Type)); } }

    private string _target = "";
    public string Target { get => _target; set { _target = value; OnPropertyChanged(nameof(Target)); } }

    private string _method = "GET";
    public string Method { get => _method; set { _method = value; OnPropertyChanged(nameof(Method)); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}