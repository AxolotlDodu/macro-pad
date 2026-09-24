namespace MacroPad.Host;

/// <summary>
/// target == null ou vide -> passe à la page suivante (cycle).
/// target == nom de page -> saute directement à cette page.
/// C'est cette 2e forme qui te permettra plus tard d'avoir des touches dédiées
/// "va à la page Jeu" plutôt que de cycler.
/// </summary>
public class SwitchPageAction : IAction
{
    private readonly IPageSwitcher _pageSwitcher;
    private readonly string? _targetPage;

    public SwitchPageAction(IPageSwitcher pageSwitcher, string? targetPage)
    {
        _pageSwitcher = pageSwitcher;
        _targetPage = string.IsNullOrWhiteSpace(targetPage) ? null : targetPage;
    }

    public void Execute()
    {
        if (_targetPage is null) _pageSwitcher.CycleNext();
        else _pageSwitcher.SwitchToPage(_targetPage);
    }
}
