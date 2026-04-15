using Payroll.Domain.Common;

namespace Payroll.Domain.Employees;

public sealed class WorkTimeSupplementSettings
{
    public static WorkTimeSupplementSettings Empty { get; } = new(null, null, null);

    public decimal? NightSupplementRate { get; }
    public decimal? SundaySupplementRate { get; }
    public decimal? HolidaySupplementRate { get; }

    public WorkTimeSupplementSettings(
        decimal? nightSupplementRate,
        decimal? sundaySupplementRate,
        decimal? holidaySupplementRate)
    {
        NightSupplementRate = ValidateOptionalRate(nightSupplementRate, nameof(nightSupplementRate));
        SundaySupplementRate = ValidateOptionalRate(sundaySupplementRate, nameof(sundaySupplementRate));
        HolidaySupplementRate = ValidateOptionalRate(holidaySupplementRate, nameof(holidaySupplementRate));
    }

    private static decimal? ValidateOptionalRate(decimal? value, string paramName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Guard.AgainstNegative(value.Value, paramName);
    }
}
