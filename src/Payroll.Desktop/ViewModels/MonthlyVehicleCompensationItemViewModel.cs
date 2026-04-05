namespace Payroll.Desktop.ViewModels;

public sealed class MonthlyVehicleCompensationItemViewModel
{
    public required Guid VehicleCompensationId { get; init; }
    public required DateOnly CompensationDate { get; init; }
    public required decimal AmountChf { get; init; }
    public required string Description { get; init; }
    public required string Summary { get; init; }
}
