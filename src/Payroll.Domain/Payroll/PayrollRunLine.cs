using Payroll.Domain.Common;

namespace Payroll.Domain.Payroll;

public sealed class PayrollRunLine : AuditableEntity
{
    private PayrollRunLine()
    {
        Code = string.Empty;
        Description = string.Empty;
    }

    private PayrollRunLine(
        Guid employeeId,
        PayrollLineType lineType,
        PayrollLineValueOrigin valueOrigin,
        string code,
        string description,
        PayrollLineUnit unit,
        decimal? quantity,
        decimal? rateChf,
        decimal amountChf)
    {
        EmployeeId = employeeId;
        LineType = lineType;
        ValueOrigin = valueOrigin;
        Code = Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));
        Unit = unit;
        Quantity = quantity;
        RateChf = rateChf;
        AmountChf = amountChf;
    }

    public Guid EmployeeId { get; private set; }
    public PayrollLineType LineType { get; private set; }
    public PayrollLineValueOrigin ValueOrigin { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public PayrollLineUnit Unit { get; private set; }
    public decimal? Quantity { get; private set; }
    public decimal? RateChf { get; private set; }
    public decimal AmountChf { get; private set; }

    public static PayrollRunLine CreateCalculatedHourlyLine(
        Guid employeeId,
        PayrollLineType lineType,
        string code,
        string description,
        decimal hours,
        decimal hourlyRateChf)
    {
        var safeHours = Guard.AgainstNegative(hours, nameof(hours));
        var safeRate = Guard.AgainstZeroOrNegative(hourlyRateChf, nameof(hourlyRateChf));

        return new PayrollRunLine(
            employeeId,
            lineType,
            PayrollLineValueOrigin.Calculated,
            code,
            description,
            PayrollLineUnit.Hours,
            safeHours,
            safeRate,
            safeHours * safeRate);
    }

    public static PayrollRunLine CreateDirectChfLine(
        Guid employeeId,
        PayrollLineType lineType,
        string code,
        string description,
        decimal amountChf)
    {
        var safeAmount = Guard.AgainstNegative(amountChf, nameof(amountChf));

        return new PayrollRunLine(
            employeeId,
            lineType,
            PayrollLineValueOrigin.Direct,
            code,
            description,
            PayrollLineUnit.Chf,
            null,
            null,
            safeAmount);
    }

    public static PayrollRunLine CreateCalculatedFixedDeduction(
        Guid employeeId,
        PayrollLineType lineType,
        string code,
        string description,
        decimal amountChf)
    {
        var safeAmount = Guard.AgainstNegative(amountChf, nameof(amountChf));

        return new PayrollRunLine(
            employeeId,
            lineType,
            PayrollLineValueOrigin.Calculated,
            code,
            description,
            PayrollLineUnit.Chf,
            null,
            null,
            -safeAmount);
    }

    public static PayrollRunLine CreateCalculatedPercentageDeduction(
        Guid employeeId,
        PayrollLineType lineType,
        string code,
        string description,
        decimal basisChf,
        decimal ratePercent)
    {
        var safeBasis = Guard.AgainstNegative(basisChf, nameof(basisChf));
        if (ratePercent < 0m || ratePercent > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(ratePercent), "Rate must be between 0 and 100.");
        }

        return new PayrollRunLine(
            employeeId,
            lineType,
            PayrollLineValueOrigin.Calculated,
            code,
            description,
            PayrollLineUnit.Percent,
            safeBasis,
            ratePercent,
            -(safeBasis * ratePercent / 100m));
    }

    public static PayrollRunLine CreateManualChfLine(
        Guid employeeId,
        PayrollLineType lineType,
        string code,
        string description,
        decimal amountChf)
    {
        return new PayrollRunLine(
            employeeId,
            lineType,
            PayrollLineValueOrigin.Direct,
            code,
            description,
            PayrollLineUnit.Chf,
            null,
            null,
            amountChf);
    }
}
