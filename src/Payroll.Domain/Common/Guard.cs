namespace Payroll.Domain.Common;

internal static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", paramName);
        }

        return value.Trim();
    }

    public static decimal AgainstNegative(decimal value, string paramName)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value cannot be negative.");
        }

        return value;
    }

    public static decimal AgainstZeroOrNegative(decimal value, string paramName)
    {
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");
        }

        return value;
    }

    public static decimal AgainstRateOutOfRange(decimal value, string paramName)
    {
        if (value < 0m || value > 1m)
        {
            throw new ArgumentOutOfRangeException(paramName, "Rate must be between 0 and 1.");
        }

        return value;
    }

    public static void AgainstInvalidPeriod(DateOnly validFrom, DateOnly? validTo, string paramName)
    {
        if (validTo.HasValue && validTo.Value < validFrom)
        {
            throw new ArgumentException("End date cannot be before start date.", paramName);
        }
    }
}
