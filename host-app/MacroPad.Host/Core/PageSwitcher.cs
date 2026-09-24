namespace MacroPad.Host;

/// <summary>
/// Point unique de vérité pour la page active. Utilisé à la fois par le binding
/// manuel "switch-page" (bit 9 réservé, ou toute autre touche configurée explicitement)
/// et par l'AppFocusWatcher (bascule automatique selon la fenêtre au premier plan).
/// </summary>
public class PageSwitcher : IPageSwitcher
{
    private List<PageConfig> _pages;
    private readonly object _lock = new();
    private int _currentIndex;

    public PageSwitcher(List<PageConfig> pages)
    {
        if (pages.Count == 0) throw new ArgumentException("Au moins une page est requise.");
        _pages = pages;
        _currentIndex = 0;
    }

    public string CurrentPageName
    {
        get { lock (_lock) return _pages[_currentIndex].Name; }
    }

    public event Action<string>? PageChanged;

    public void SwitchToPage(string pageName)
    {
        string? resolved = null;
        lock (_lock)
        {
            var idx = _pages.FindIndex(p => string.Equals(p.Name, pageName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0 || idx == _currentIndex) return;
            _currentIndex = idx;
            resolved = _pages[idx].Name;
        }

        Console.WriteLine($"[PageSwitcher] Page active -> {resolved}");
        PageChanged?.Invoke(resolved!);
    }

    public void CycleNext()
    {
        string next;
        lock (_lock)
        {
            _currentIndex = (_currentIndex + 1) % _pages.Count;
            next = _pages[_currentIndex].Name;
        }

        Console.WriteLine($"[PageSwitcher] Page active -> {next} (cycle)");
        PageChanged?.Invoke(next);
    }

    public void UpdatePages(List<PageConfig> pages)
    {
        if (pages.Count == 0) throw new ArgumentException("Au moins une page est requise.");

        lock (_lock)
        {
            var currentName = _pages.Count > _currentIndex ? _pages[_currentIndex].Name : null;
            _pages = pages;
            var idx = currentName is not null
                ? _pages.FindIndex(p => string.Equals(p.Name, currentName, StringComparison.OrdinalIgnoreCase))
                : -1;
            _currentIndex = idx >= 0 ? idx : 0;
        }
    }
}
