using Payroll.Domain.MonthlyRecords;

namespace Payroll.Infrastructure.Payroll;

internal static class SalaryAdvancePayrollMonthFilter
{
    public static IReadOnlyCollection<SalaryAdvance> FilterForMonth(
        IReadOnlyCollection<SalaryAdvance> salaryAdvances,
        Guid employeeId,
        int year,
        int month)
    {
        ArgumentNullException.ThrowIfNull(salaryAdvances);

        return salaryAdvances
            .Where(advance => advance.EmployeeId == employeeId
                && ((advance.Year == year && advance.Month == month)
                    || advance.Settlements.Any(settlement => settlement.Year == year && settlement.Month == month)))
            .Select(advance => CloneUpToMonth(advance, year, month))
            .ToArray();
    }

    private static SalaryAdvance CloneUpToMonth(SalaryAdvance advance, int year, int month)
    {
        var clone = new SalaryAdvance(
            advance.EmployeeMonthlyRecordId,
            advance.EmployeeId,
            advance.Year,
            advance.Month,
            advance.AmountChf,
            advance.Note);

        foreach (var settlement in advance.Settlements
                     .Where(settlement => IsOnOrBeforeMonth(settlement.Year, settlement.Month, year, month))
                     .OrderBy(settlement => settlement.Year)
                     .ThenBy(settlement => settlement.Month)
                     .ThenBy(settlement => settlement.Id))
        {
            clone.SaveSettlement(
                null,
                settlement.EmployeeMonthlyRecordId,
                settlement.Year,
                settlement.Month,
                settlement.AmountChf,
                settlement.Note);
        }

        return clone;
    }

    private static bool IsOnOrBeforeMonth(int candidateYear, int candidateMonth, int year, int month)
    {
        return candidateYear < year || (candidateYear == year && candidateMonth <= month);
    }
}
