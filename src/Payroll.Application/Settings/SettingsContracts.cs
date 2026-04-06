namespace Payroll.Application.Settings;

public sealed record PayrollSettingsDto(
    decimal? NightSupplementRate,
    decimal? SundaySupplementRate,
    decimal? HolidaySupplementRate,
    decimal AhvIvEoRate,
    decimal AlvRate,
    decimal SicknessAccidentInsuranceRate,
    decimal TrainingAndHolidayRate,
    decimal VacationCompensationRate,
    decimal VehiclePauschalzone1RateChf,
    decimal VehiclePauschalzone2RateChf,
    decimal VehicleRegiezone1RateChf);

public sealed record SavePayrollSettingsCommand(
    decimal? NightSupplementRate,
    decimal? SundaySupplementRate,
    decimal? HolidaySupplementRate,
    decimal AhvIvEoRate,
    decimal AlvRate,
    decimal SicknessAccidentInsuranceRate,
    decimal TrainingAndHolidayRate,
    decimal VacationCompensationRate,
    decimal VehiclePauschalzone1RateChf,
    decimal VehiclePauschalzone2RateChf,
    decimal VehicleRegiezone1RateChf);
