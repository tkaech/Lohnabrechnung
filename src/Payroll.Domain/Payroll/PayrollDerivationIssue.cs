using Payroll.Domain.Common;

namespace Payroll.Domain.Payroll;

public sealed class PayrollDerivationIssue
{
    public PayrollDerivationIssue(string code, string message)
    {
        Code = Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Message = Guard.AgainstNullOrWhiteSpace(message, nameof(message));
    }

    public string Code { get; }
    public string Message { get; }
}
