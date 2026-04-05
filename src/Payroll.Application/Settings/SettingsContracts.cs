namespace Payroll.Application.Settings;

public sealed record PayrollSettingsDto(
    decimal? NightSupplementRate,
    decimal? SundaySupplementRate,
    decimal? HolidaySupplementRate);

public sealed record SavePayrollSettingsCommand(
    decimal? NightSupplementRate,
    decimal? SundaySupplementRate,
    decimal? HolidaySupplementRate);
