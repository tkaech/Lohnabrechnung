using Payroll.Domain.Employees;
using Payroll.Domain.Settings;

namespace Payroll.Domain.MonthlyRecords;

public sealed class PayrollParameterSnapshot
{
    internal PayrollParameterSnapshot()
    {
        IsInitialized = false;
        CapturedAtUtc = DateTimeOffset.MinValue;
    }

    private PayrollParameterSnapshot(
        DateTimeOffset capturedAtUtc,
        decimal? nightSupplementRate,
        decimal? sundaySupplementRate,
        decimal? holidaySupplementRate,
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
        IsInitialized = true;
        CapturedAtUtc = capturedAtUtc;
        NightSupplementRate = nightSupplementRate;
        SundaySupplementRate = sundaySupplementRate;
        HolidaySupplementRate = holidaySupplementRate;
        AhvIvEoRate = ahvIvEoRate;
        AlvRate = alvRate;
        SicknessAccidentInsuranceRate = sicknessAccidentInsuranceRate;
        TrainingAndHolidayRate = trainingAndHolidayRate;
        VacationCompensationRate = vacationCompensationRate;
        VacationCompensationRateAge50Plus = vacationCompensationRateAge50Plus;
        VehiclePauschalzone1RateChf = vehiclePauschalzone1RateChf;
        VehiclePauschalzone2RateChf = vehiclePauschalzone2RateChf;
        VehicleRegiezone1RateChf = vehicleRegiezone1RateChf;
    }

    public bool IsInitialized { get; private set; }
    public DateTimeOffset CapturedAtUtc { get; private set; }
    public decimal? NightSupplementRate { get; private set; }
    public decimal? SundaySupplementRate { get; private set; }
    public decimal? HolidaySupplementRate { get; private set; }
    public decimal AhvIvEoRate { get; private set; }
    public decimal AlvRate { get; private set; }
    public decimal SicknessAccidentInsuranceRate { get; private set; }
    public decimal TrainingAndHolidayRate { get; private set; }
    public decimal VacationCompensationRate { get; private set; }
    public decimal VacationCompensationRateAge50Plus { get; private set; }
    public decimal VehiclePauschalzone1RateChf { get; private set; }
    public decimal VehiclePauschalzone2RateChf { get; private set; }
    public decimal VehicleRegiezone1RateChf { get; private set; }

    public static PayrollParameterSnapshot Create(PayrollSettings payrollSettings)
    {
        ArgumentNullException.ThrowIfNull(payrollSettings);

        return new PayrollParameterSnapshot(
            DateTimeOffset.UtcNow,
            payrollSettings.WorkTimeSupplementSettings.NightSupplementRate,
            payrollSettings.WorkTimeSupplementSettings.SundaySupplementRate,
            payrollSettings.WorkTimeSupplementSettings.HolidaySupplementRate,
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

    public PayrollSettings ToPayrollSettings()
    {
        return new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(
                NightSupplementRate,
                SundaySupplementRate,
                HolidaySupplementRate),
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
}
