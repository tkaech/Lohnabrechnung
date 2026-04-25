using System.Collections.ObjectModel;
using Payroll.Application.Layout;

namespace Payroll.Desktop.ViewModels;

public sealed class LayoutParameterHelpGroupViewModel : ViewModelBase
{
    public LayoutParameterHelpGroupViewModel(string title, string summary, IEnumerable<LayoutParameterHelpItemDto> parameters)
    {
        Title = title;
        Summary = summary;
        Parameters = new ObservableCollection<LayoutParameterHelpItemViewModel>(
            parameters.Select(parameter => new LayoutParameterHelpItemViewModel(parameter)));
    }

    public string Title { get; }

    public string Summary { get; }

    public ObservableCollection<LayoutParameterHelpItemViewModel> Parameters { get; }
}
