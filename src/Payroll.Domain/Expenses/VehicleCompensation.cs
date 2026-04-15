using Payroll.Domain.Common;

namespace Payroll.Domain.Expenses;

public sealed class VehicleCompensation : AuditableEntity
{
    private VehicleCompensation()
    {
        Description = string.Empty;
    }

    public Guid EmployeeMonthlyRecordId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly CompensationDate { get; private set; }
    public decimal AmountChf { get; private set; }
    public string Description { get; private set; } = string.Empty;

    public VehicleCompensation(Guid employeeId, DateOnly compensationDate, decimal amountChf, string description)
        : this(Guid.Empty, employeeId, compensationDate, amountChf, description)
    {
    }

    public VehicleCompensation(Guid employeeMonthlyRecordId, Guid employeeId, DateOnly compensationDate, decimal amountChf, string description)
    {
        EmployeeId = employeeId;
        EmployeeMonthlyRecordId = employeeMonthlyRecordId;
        Update(compensationDate, amountChf, description);
    }

    public void Update(DateOnly compensationDate, decimal amountChf, string description)
    {
        CompensationDate = compensationDate;
        AmountChf = Guard.AgainstNegative(amountChf, nameof(amountChf));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));
        Touch();
    }
}
