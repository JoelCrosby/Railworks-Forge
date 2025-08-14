using System.Globalization;
using System.Reactive;

using CommunityToolkit.Mvvm.ComponentModel;

using Echoes;

using RailworksForge.Core.Config;

using ReactiveUI;

namespace RailworksForge.ViewModels.MenuItems;

public partial class LanguageMenuItem : ViewModelBase
{
    public string Header { get; }

    private string Culture { get; }

    [ObservableProperty]
    private bool _isSelected;

    public ReactiveCommand<Unit, Unit> SetCultureCommand { get; }

    public LanguageMenuItem(string header, string culture, bool isSelected)
    {
        Header = header;
        Culture = culture;
        IsSelected = isSelected;

        SetCultureCommand = ReactiveCommand.Create(() =>
        {
            TranslationProvider.SetCulture(CultureInfo.GetCultureInfo(Culture));

            Configuration.Set(Configuration.Get() with
            {
                Language = Culture,
            });

            IsSelected = true;
        });
    }
}
