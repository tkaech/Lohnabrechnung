using Payroll.Domain.Common;

namespace Payroll.Domain.Tax;

public sealed class TaxRule : AuditableEntity
{
    public string Canton { get; private set; }
    public string TariffCode { get; private set; }
    public decimal Rate { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }

    public TaxRule(
        string canton,
        string tariffCode,
        decimal rate,
        DateOnly validFrom,
        DateOnly? validTo = null)
    {
        Guard.AgainstInvalidPeriod(validFrom, validTo, nameof(validTo));

        Canton = Guard.AgainstNullOrWhiteSpace(canton, nameof(canton));
        TariffCode = Guard.AgainstNullOrWhiteSpace(tariffCode, nameof(tariffCode));
        Rate = Guard.AgainstRateOutOfRange(rate, nameof(rate));
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public bool IsValidOn(DateOnly date)
    {
        return date >= ValidFrom && (!ValidTo.HasValue || date <= ValidTo.Value);
    }

    public decimal CalculateTax(decimal taxableAmountChf)
    {
        return Guard.AgainstNegative(taxableAmountChf, nameof(taxableAmountChf)) * Rate;
    }
}
