namespace Payroll.Desktop.ViewModels;

public sealed class EmployeeListItemViewModel
{
    public required Guid EmployeeId { get; init; }
    public required string PersonnelNumber { get; init; }
    public required string FullName { get; init; }
    public required string StatusSummary { get; init; }
    public required string ContactSummary { get; init; }
    public required string ContractSummary { get; init; }
}
