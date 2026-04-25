using System.Collections.ObjectModel;
using Payroll.Application.Layout;

namespace Payroll.Desktop.ViewModels;

public sealed class LayoutParameterHelpViewModel : ViewModelBase
{
    private HelpNavigationItemViewModel? _selectedNavigationItem;

    public LayoutParameterHelpViewModel()
    {
        NavigationItems =
        [
            new HelpNavigationItemViewModel(HelpSection.Overview, "Ueberblick"),
            new HelpNavigationItemViewModel(HelpSection.LayoutParameters, "Layout-Parameter")
        ];

        Groups = new ObservableCollection<LayoutParameterHelpGroupViewModel>(
            LayoutParameterHelpCatalog.GetGroups()
                .Select(group => new LayoutParameterHelpGroupViewModel(
                    group.Title,
                    group.Summary,
                    group.Parameters)));

        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<HelpNavigationItemViewModel> NavigationItems { get; }

    public ObservableCollection<LayoutParameterHelpGroupViewModel> Groups { get; }

    public HelpNavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value))
            {
                RaisePropertyChanged(nameof(ShowOverviewSection));
                RaisePropertyChanged(nameof(ShowLayoutParametersSection));
            }
        }
    }

    public bool ShowOverviewSection => SelectedNavigationItem?.Section != HelpSection.LayoutParameters;

    public bool ShowLayoutParametersSection => SelectedNavigationItem?.Section == HelpSection.LayoutParameters;

    public void ApplyCurrentValues(IReadOnlyDictionary<string, string> values)
    {
        foreach (var parameter in Groups.SelectMany(group => group.Parameters))
        {
            if (values.TryGetValue(parameter.Name, out var value))
            {
                parameter.ApplyCurrentValue(value);
            }
        }
    }
}
