namespace Payroll.Domain.Payroll;

public sealed class PayrollRunLineDerivationResult
{
    public PayrollRunLineDerivationResult(
        IReadOnlyCollection<PayrollRunLine> lines,
        IReadOnlyCollection<PayrollDerivationIssue> issues)
    {
        Lines = lines;
        Issues = issues;
    }

    public IReadOnlyCollection<PayrollRunLine> Lines { get; }
    public IReadOnlyCollection<PayrollDerivationIssue> Issues { get; }
}
