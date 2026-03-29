using System.Collections.ObjectModel;
using Payroll.Application.Employees;

namespace Payroll.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<EmployeeItemViewModel> Employees { get; } = new();

    public string Title => "Lohnverwaltung Schweiz";

    public string StatusText => "Minimaler Startpunkt mit Domain-, Application-, Infrastructure- und UI-Schicht";

    public MainWindowViewModel(EmployeeQueries employeeQueries)
    {
        var employees = employeeQueries.GetActiveEmployeesAsync().GetAwaiter().GetResult();

        foreach (var employee in employees)
        {
            Employees.Add(new EmployeeItemViewModel
            {
                EmployeeNumber = employee.EmployeeNumber,
                FullName = employee.FullName,
                EmploymentType = employee.EmploymentType,
                MonthlySalary = employee.MonthlySalary,
                HourlyRate = employee.HourlyRate
            });
        }
    }
}
