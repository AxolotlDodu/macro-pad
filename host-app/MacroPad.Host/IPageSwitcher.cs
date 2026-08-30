namespace MacroPad.Host;

public interface IPageSwitcher
{
    string CurrentPageName { get; }

    /// <summary>Déclenché après un changement de page effectif, avec le nom de la nouvelle page.</summary>
    event Action<string>? PageChanged;

    void SwitchToPage(string pageName);
    void CycleNext();
}
