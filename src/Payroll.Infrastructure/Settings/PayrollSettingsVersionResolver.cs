using Payroll.Domain.Employees;
using Payroll.Domain.Settings;

namespace Payroll.Infrastructure.Settings;

internal static class PayrollSettingsVersionResolver
{
    public static PayrollSettings ResolveForDate(
        PayrollSettings fallbackSettings,
        IReadOnlyCollection<PayrollGeneralSettingsVersion> generalVersions,
        IReadOnlyCollection<PayrollHourlySettingsVersion> hourlyVersions,
        DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(fallbackSettings);
        ArgumentNullException.ThrowIfNull(generalVersions);
        ArgumentNullException.ThrowIfNull(hourlyVersions);

        var general = ResolveVersion(generalVersions, referenceDate);
        var hourly = ResolveVersion(hourlyVersions, referenceDate);

        return new PayrollSettings(
            workTimeSupplementSettings: hourly is null
                ? fallbackSettings.WorkTimeSupplementSettings
                : new WorkTimeSupplementSettings(
                    hourly.NightSupplementRate,
                    hourly.SundaySupplementRate,
                    hourly.HolidaySupplementRate),
            ahvIvEoRate: general?.AhvIvEoRate ?? fallbackSettings.AhvIvEoRate,
            alvRate: general?.AlvRate ?? fallbackSettings.AlvRate,
            sicknessAccidentInsuranceRate: general?.SicknessAccidentInsuranceRate ?? fallbackSettings.SicknessAccidentInsuranceRate,
            trainingAndHolidayRate: general?.TrainingAndHolidayRate ?? fallbackSettings.TrainingAndHolidayRate,
            vacationCompensationRate: hourly?.VacationCompensationRate ?? fallbackSettings.VacationCompensationRate,
            vacationCompensationRateAge50Plus: hourly?.VacationCompensationRateAge50Plus ?? fallbackSettings.VacationCompensationRateAge50Plus,
            vehiclePauschalzone1RateChf: hourly?.VehiclePauschalzone1RateChf ?? fallbackSettings.VehiclePauschalzone1RateChf,
            vehiclePauschalzone2RateChf: hourly?.VehiclePauschalzone2RateChf ?? fallbackSettings.VehiclePauschalzone2RateChf,
            vehicleRegiezone1RateChf: hourly?.VehicleRegiezone1RateChf ?? fallbackSettings.VehicleRegiezone1RateChf);
    }

    private static T? ResolveVersion<T>(IReadOnlyCollection<T> versions, DateOnly referenceDate)
        where T : PayrollCalculationSettingsVersionBase
    {
        return versions
            .Where(item => item.ValidFrom <= referenceDate && (!item.ValidTo.HasValue || item.ValidTo.Value >= referenceDate))
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault()
            ?? versions
                .OrderByDescending(item => item.ValidFrom)
                .ThenByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault();
    }
}
