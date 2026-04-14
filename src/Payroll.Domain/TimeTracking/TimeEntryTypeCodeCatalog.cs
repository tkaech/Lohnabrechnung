namespace Payroll.Domain.TimeTracking;

public static class TimeEntryTypeCodeCatalog
{
    public const string StandardWork = "1001";
    public const string NightWork = "1002";
    public const string SundayWork = "1003";
    public const string HolidayWork = "1004";

    public static string Normalize(string? typeCode)
    {
        return string.IsNullOrWhiteSpace(typeCode)
            ? StandardWork
            : typeCode.Trim().ToUpperInvariant();
    }

    public static bool IsNight(string? typeCode)
    {
        var normalized = Normalize(typeCode);
        return normalized == NightWork || normalized.Contains("NIGHT", StringComparison.Ordinal) || normalized.Contains("NACHT", StringComparison.Ordinal);
    }

    public static bool IsSunday(string? typeCode)
    {
        var normalized = Normalize(typeCode);
        return normalized == SundayWork || normalized.Contains("SUNDAY", StringComparison.Ordinal) || normalized.Contains("SONNTAG", StringComparison.Ordinal);
    }

    public static bool IsHoliday(string? typeCode)
    {
        var normalized = Normalize(typeCode);
        return normalized == HolidayWork || normalized.Contains("HOLIDAY", StringComparison.Ordinal) || normalized.Contains("FEIERTAG", StringComparison.Ordinal);
    }
}
