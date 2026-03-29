namespace Payroll.Desktop.ViewModels;

public sealed class EmployeeItemViewModel
{
    public string EmployeeNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string EmploymentType { get; init; } = string.Empty;
    public decimal MonthlySalary { get; init; }
    public decimal HourlyRate { get; init; }
}
