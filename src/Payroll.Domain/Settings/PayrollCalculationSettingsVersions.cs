using Payroll.Domain.Common;

namespace Payroll.Domain.Settings;

public abstract class PayrollCalculationSettingsVersionBase : AuditableEntity
{
    protected PayrollCalculationSettingsVersionBase()
    {
        ValidFrom = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }

    public void UpdateValidity(DateOnly validFrom, DateOnly? validTo)
    {
        var normalizedValidFrom = NormalizeToMonthStart(validFrom);
        var normalizedValidTo = NormalizeToMonthEnd(validTo);

        if (normalizedValidTo.HasValue && normalizedValidTo.Value < normalizedValidFrom)
        {
            throw new InvalidOperationException("Gueltig bis darf nicht vor Gueltig ab liegen.");
        }

        ValidFrom = normalizedValidFrom;
        ValidTo = normalizedValidTo;
        Touch();
    }

    protected static DateOnly NormalizeToMonthStart(DateOnly value)
    {
        return new DateOnly(value.Year, value.Month, 1);
    }

    protected static DateOnly? NormalizeToMonthEnd(DateOnly? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var date = value.Value;
        return new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }
}

public sealed class PayrollGeneralSettingsVersion : PayrollCalculationSettingsVersionBase
{
    public decimal AhvIvEoRate { get; private set; }
    public decimal AlvRate { get; private set; }
    public decimal SicknessAccidentInsuranceRate { get; private set; }
    public decimal TrainingAndHolidayRate { get; private set; }

    public void UpdateRates(
        decimal ahvIvEoRate,
        decimal alvRate,
        decimal sicknessAccidentInsuranceRate,
        decimal trainingAndHolidayRate)
    {
        AhvIvEoRate = Guard.AgainstNegative(ahvIvEoRate, nameof(ahvIvEoRate));
        AlvRate = Guard.AgainstNegative(alvRate, nameof(alvRate));
        SicknessAccidentInsuranceRate = Guard.AgainstNegative(sicknessAccidentInsuranceRate, nameof(sicknessAccidentInsuranceRate));
        TrainingAndHolidayRate = Guard.AgainstNegative(trainingAndHolidayRate, nameof(trainingAndHolidayRate));
        Touch();
    }
}

public sealed class PayrollHourlySettingsVersion : PayrollCalculationSettingsVersionBase
{
    public decimal? NightSupplementRate { get; private set; }
    public decimal? SundaySupplementRate { get; private set; }
    public decimal? HolidaySupplementRate { get; private set; }
    public decimal VacationCompensationRate { get; private set; }
    public decimal VacationCompensationRateAge50Plus { get; private set; }
    public decimal VehiclePauschalzone1RateChf { get; private set; }
    public decimal VehiclePauschalzone2RateChf { get; private set; }
    public decimal VehicleRegiezone1RateChf { get; private set; }

    public void UpdateRates(
        decimal? nightSupplementRate,
        decimal? sundaySupplementRate,
        decimal? holidaySupplementRate,
        decimal vacationCompensationRate,
        decimal vacationCompensationRateAge50Plus,
        decimal vehiclePauschalzone1RateChf,
        decimal vehiclePauschalzone2RateChf,
        decimal vehicleRegiezone1RateChf)
    {
        NightSupplementRate = ValidateOptionalRate(nightSupplementRate, nameof(nightSupplementRate));
        SundaySupplementRate = ValidateOptionalRate(sundaySupplementRate, nameof(sundaySupplementRate));
        HolidaySupplementRate = ValidateOptionalRate(holidaySupplementRate, nameof(holidaySupplementRate));
        VacationCompensationRate = Guard.AgainstNegative(vacationCompensationRate, nameof(vacationCompensationRate));
        VacationCompensationRateAge50Plus = Guard.AgainstNegative(vacationCompensationRateAge50Plus, nameof(vacationCompensationRateAge50Plus));
        VehiclePauschalzone1RateChf = Guard.AgainstNegative(vehiclePauschalzone1RateChf, nameof(vehiclePauschalzone1RateChf));
        VehiclePauschalzone2RateChf = Guard.AgainstNegative(vehiclePauschalzone2RateChf, nameof(vehiclePauschalzone2RateChf));
        VehicleRegiezone1RateChf = Guard.AgainstNegative(vehicleRegiezone1RateChf, nameof(vehicleRegiezone1RateChf));
        Touch();
    }

    private static decimal? ValidateOptionalRate(decimal? value, string paramName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Guard.AgainstNegative(value.Value, paramName);
    }
}

public sealed class PayrollMonthlySalarySettingsVersion : PayrollCalculationSettingsVersionBase
{
    public void MarkPrepared()
    {
        Touch();
    }
}
