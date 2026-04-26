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
}
