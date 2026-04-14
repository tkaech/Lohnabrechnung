using Payroll.Domain.Common;
using Payroll.Domain.Employees;

namespace Payroll.Domain.Settings;

public sealed class PayrollCalculationSettingsVersion : AuditableEntity
{
    private PayrollCalculationSettingsVersion()
    {
    }

    public PayrollCalculationSettingsVersion(
        DateOnly validFrom,
        DateOnly? validTo,
        WorkTimeSupplementSettings workTimeSupplementSettings,
        decimal ahvIvEoRate,
        decimal alvRate,
        decimal sicknessAccidentInsuranceRate,
        decimal trainingAndHolidayRate,
        decimal vacationCompensationRate,
        decimal vacationCompensationRateAge50Plus,
        decimal vehiclePauschalzone1RateChf,
        decimal vehiclePauschalzone2RateChf,
        decimal vehicleRegiezone1RateChf)
    {
        Guard.AgainstInvalidPeriod(validFrom, validTo, nameof(validTo));

        ValidFrom = validFrom;
        ValidTo = validTo;
        UpdateRates(
            workTimeSupplementSettings,
            ahvIvEoRate,
            alvRate,
            sicknessAccidentInsuranceRate,
            trainingAndHolidayRate,
            vacationCompensationRate,
            vacationCompensationRateAge50Plus,
            vehiclePauschalzone1RateChf,
            vehiclePauschalzone2RateChf,
            vehicleRegiezone1RateChf);
    }

    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public WorkTimeSupplementSettings WorkTimeSupplementSettings { get; private set; } = WorkTimeSupplementSettings.Empty;
    public decimal AhvIvEoRate { get; private set; }
    public decimal AlvRate { get; private set; }
    public decimal SicknessAccidentInsuranceRate { get; private set; }
    public decimal TrainingAndHolidayRate { get; private set; }
    public decimal VacationCompensationRate { get; private set; }
    public decimal VacationCompensationRateAge50Plus { get; private set; }
    public decimal VehiclePauschalzone1RateChf { get; private set; }
    public decimal VehiclePauschalzone2RateChf { get; private set; }
    public decimal VehicleRegiezone1RateChf { get; private set; }

    public bool IsActiveOn(DateOnly date)
    {
        return date >= ValidFrom && (!ValidTo.HasValue || date <= ValidTo.Value);
    }

    public void UpdatePeriod(DateOnly validFrom, DateOnly? validTo)
    {
        Guard.AgainstInvalidPeriod(validFrom, validTo, nameof(validTo));
        ValidFrom = validFrom;
        ValidTo = validTo;
        Touch();
    }

    public void UpdateRates(
        WorkTimeSupplementSettings workTimeSupplementSettings,
        decimal ahvIvEoRate,
        decimal alvRate,
        decimal sicknessAccidentInsuranceRate,
        decimal trainingAndHolidayRate,
        decimal vacationCompensationRate,
        decimal vacationCompensationRateAge50Plus,
        decimal vehiclePauschalzone1RateChf,
        decimal vehiclePauschalzone2RateChf,
        decimal vehicleRegiezone1RateChf)
    {
        ArgumentNullException.ThrowIfNull(workTimeSupplementSettings);

        WorkTimeSupplementSettings = workTimeSupplementSettings;
        AhvIvEoRate = Guard.AgainstNegative(ahvIvEoRate, nameof(ahvIvEoRate));
        AlvRate = Guard.AgainstNegative(alvRate, nameof(alvRate));
        SicknessAccidentInsuranceRate = Guard.AgainstNegative(sicknessAccidentInsuranceRate, nameof(sicknessAccidentInsuranceRate));
        TrainingAndHolidayRate = Guard.AgainstNegative(trainingAndHolidayRate, nameof(trainingAndHolidayRate));
        VacationCompensationRate = Guard.AgainstNegative(vacationCompensationRate, nameof(vacationCompensationRate));
        VacationCompensationRateAge50Plus = Guard.AgainstNegative(vacationCompensationRateAge50Plus, nameof(vacationCompensationRateAge50Plus));
        VehiclePauschalzone1RateChf = Guard.AgainstNegative(vehiclePauschalzone1RateChf, nameof(vehiclePauschalzone1RateChf));
        VehiclePauschalzone2RateChf = Guard.AgainstNegative(vehiclePauschalzone2RateChf, nameof(vehiclePauschalzone2RateChf));
        VehicleRegiezone1RateChf = Guard.AgainstNegative(vehicleRegiezone1RateChf, nameof(vehicleRegiezone1RateChf));
        Touch();
    }

    public PayrollSettings ToPayrollSettings()
    {
        return new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(
                WorkTimeSupplementSettings.NightSupplementRate,
                WorkTimeSupplementSettings.SundaySupplementRate,
                WorkTimeSupplementSettings.HolidaySupplementRate),
            ahvIvEoRate: AhvIvEoRate,
            alvRate: AlvRate,
            sicknessAccidentInsuranceRate: SicknessAccidentInsuranceRate,
            trainingAndHolidayRate: TrainingAndHolidayRate,
            vacationCompensationRate: VacationCompensationRate,
            vacationCompensationRateAge50Plus: VacationCompensationRateAge50Plus,
            vehiclePauschalzone1RateChf: VehiclePauschalzone1RateChf,
            vehiclePauschalzone2RateChf: VehiclePauschalzone2RateChf,
            vehicleRegiezone1RateChf: VehicleRegiezone1RateChf);
    }

    public static PayrollCalculationSettingsVersion Create(
        DateOnly validFrom,
        DateOnly? validTo,
        PayrollSettings payrollSettings)
    {
        ArgumentNullException.ThrowIfNull(payrollSettings);

        return new PayrollCalculationSettingsVersion(
            validFrom,
            validTo,
            new WorkTimeSupplementSettings(
                payrollSettings.WorkTimeSupplementSettings.NightSupplementRate,
                payrollSettings.WorkTimeSupplementSettings.SundaySupplementRate,
                payrollSettings.WorkTimeSupplementSettings.HolidaySupplementRate),
            payrollSettings.AhvIvEoRate,
            payrollSettings.AlvRate,
            payrollSettings.SicknessAccidentInsuranceRate,
            payrollSettings.TrainingAndHolidayRate,
            payrollSettings.VacationCompensationRate,
            payrollSettings.VacationCompensationRateAge50Plus,
            payrollSettings.VehiclePauschalzone1RateChf,
            payrollSettings.VehiclePauschalzone2RateChf,
            payrollSettings.VehicleRegiezone1RateChf);
    }
}
