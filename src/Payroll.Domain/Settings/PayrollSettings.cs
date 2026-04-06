using Payroll.Domain.Common;
using Payroll.Domain.Employees;

namespace Payroll.Domain.Settings;

public sealed class PayrollSettings : AuditableEntity
{
    public const decimal DefaultAhvIvEoRate = 0.053m;
    public const decimal DefaultAlvRate = 0.011m;
    public const decimal DefaultSicknessAccidentInsuranceRate = 0.00821m;
    public const decimal DefaultTrainingAndHolidayRate = 0.00015m;
    public const decimal DefaultVacationCompensationRate = 0.1064m;

    private PayrollSettings()
    {
        WorkTimeSupplementSettings = WorkTimeSupplementSettings.Empty;
        AhvIvEoRate = DefaultAhvIvEoRate;
        AlvRate = DefaultAlvRate;
        SicknessAccidentInsuranceRate = DefaultSicknessAccidentInsuranceRate;
        TrainingAndHolidayRate = DefaultTrainingAndHolidayRate;
        VacationCompensationRate = DefaultVacationCompensationRate;
    }

    public PayrollSettings(
        WorkTimeSupplementSettings? workTimeSupplementSettings = null,
        decimal ahvIvEoRate = DefaultAhvIvEoRate,
        decimal alvRate = DefaultAlvRate,
        decimal sicknessAccidentInsuranceRate = DefaultSicknessAccidentInsuranceRate,
        decimal trainingAndHolidayRate = DefaultTrainingAndHolidayRate,
        decimal vacationCompensationRate = DefaultVacationCompensationRate,
        decimal vehiclePauschalzone1RateChf = 0m,
        decimal vehiclePauschalzone2RateChf = 0m,
        decimal vehicleRegiezone1RateChf = 0m)
    {
        WorkTimeSupplementSettings = workTimeSupplementSettings ?? WorkTimeSupplementSettings.Empty;
        UpdateDeductionAndVehicleRates(
            ahvIvEoRate,
            alvRate,
            sicknessAccidentInsuranceRate,
            trainingAndHolidayRate,
            vacationCompensationRate,
            vehiclePauschalzone1RateChf,
            vehiclePauschalzone2RateChf,
            vehicleRegiezone1RateChf);
    }

    public WorkTimeSupplementSettings WorkTimeSupplementSettings { get; private set; }
    public decimal AhvIvEoRate { get; private set; }
    public decimal AlvRate { get; private set; }
    public decimal SicknessAccidentInsuranceRate { get; private set; }
    public decimal TrainingAndHolidayRate { get; private set; }
    public decimal VacationCompensationRate { get; private set; }
    public decimal VehiclePauschalzone1RateChf { get; private set; }
    public decimal VehiclePauschalzone2RateChf { get; private set; }
    public decimal VehicleRegiezone1RateChf { get; private set; }

    public void UpdateWorkTimeSupplementSettings(WorkTimeSupplementSettings workTimeSupplementSettings)
    {
        ArgumentNullException.ThrowIfNull(workTimeSupplementSettings);

        WorkTimeSupplementSettings = workTimeSupplementSettings;
        Touch();
    }

    public void UpdateDeductionAndVehicleRates(
        decimal ahvIvEoRate,
        decimal alvRate,
        decimal sicknessAccidentInsuranceRate,
        decimal trainingAndHolidayRate,
        decimal vacationCompensationRate,
        decimal vehiclePauschalzone1RateChf,
        decimal vehiclePauschalzone2RateChf,
        decimal vehicleRegiezone1RateChf)
    {
        AhvIvEoRate = Guard.AgainstNegative(ahvIvEoRate, nameof(ahvIvEoRate));
        AlvRate = Guard.AgainstNegative(alvRate, nameof(alvRate));
        SicknessAccidentInsuranceRate = Guard.AgainstNegative(sicknessAccidentInsuranceRate, nameof(sicknessAccidentInsuranceRate));
        TrainingAndHolidayRate = Guard.AgainstNegative(trainingAndHolidayRate, nameof(trainingAndHolidayRate));
        VacationCompensationRate = Guard.AgainstNegative(vacationCompensationRate, nameof(vacationCompensationRate));
        VehiclePauschalzone1RateChf = Guard.AgainstNegative(vehiclePauschalzone1RateChf, nameof(vehiclePauschalzone1RateChf));
        VehiclePauschalzone2RateChf = Guard.AgainstNegative(vehiclePauschalzone2RateChf, nameof(vehiclePauschalzone2RateChf));
        VehicleRegiezone1RateChf = Guard.AgainstNegative(vehicleRegiezone1RateChf, nameof(vehicleRegiezone1RateChf));
        Touch();
    }
}
