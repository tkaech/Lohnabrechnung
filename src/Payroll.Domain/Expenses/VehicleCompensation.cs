using Payroll.Domain.Common;

namespace Payroll.Domain.Expenses;

public sealed class VehicleCompensation : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public DateOnly CompensationDate { get; private set; }
    public decimal AmountChf { get; private set; }
    public string Description { get; private set; }

    public VehicleCompensation(Guid employeeId, DateOnly compensationDate, decimal amountChf, string description)
    {
        EmployeeId = employeeId;
        CompensationDate = compensationDate;
        AmountChf = Guard.AgainstNegative(amountChf, nameof(amountChf));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));
    }
}
