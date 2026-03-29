namespace Payroll.Application.Employees;

public sealed record EmployeeSummaryDto(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string EmploymentType,
    decimal MonthlySalary,
    decimal HourlyRate);
