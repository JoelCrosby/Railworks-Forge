namespace RailworksForge.ViewModels.MenuItems;

public class CultureMenuItem : ViewModelBase
{
    public string Header { get; }

    public string Culture { get; }

    public CultureMenuItem(string header, string culture)
    {
        Header = header;
        Culture = culture;
    }
}
