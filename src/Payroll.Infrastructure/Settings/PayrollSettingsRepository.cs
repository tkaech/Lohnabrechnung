using Microsoft.EntityFrameworkCore;
using Payroll.Application.Settings;
using Payroll.Domain.Employees;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Settings;

public sealed class PayrollSettingsRepository : IPayrollSettingsRepository
{
    private readonly PayrollDbContext _dbContext;

    public PayrollSettingsRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PayrollSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return settings.WorkTimeSupplementSettings;
    }

    public async Task<PayrollSettingsDto> SaveAsync(SavePayrollSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        settings.UpdateWorkTimeSupplementSettings(new WorkTimeSupplementSettings(
            command.NightSupplementRate,
            command.SundaySupplementRate,
            command.HolidaySupplementRate));
        settings.UpdateDeductionAndVehicleRates(
            command.AhvIvEoRate,
            command.AlvRate,
            command.SicknessAccidentInsuranceRate,
            command.TrainingAndHolidayRate,
            command.VacationCompensationRate,
            command.VehiclePauschalzone1RateChf,
            command.VehiclePauschalzone2RateChf,
            command.VehicleRegiezone1RateChf);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    private async Task<PayrollSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.Set<PayrollSettings>().SingleOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new PayrollSettings();
        _dbContext.Set<PayrollSettings>().Add(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static PayrollSettingsDto ToDto(PayrollSettings settings)
    {
        return new PayrollSettingsDto(
            settings.WorkTimeSupplementSettings.NightSupplementRate,
            settings.WorkTimeSupplementSettings.SundaySupplementRate,
            settings.WorkTimeSupplementSettings.HolidaySupplementRate,
            settings.AhvIvEoRate,
            settings.AlvRate,
            settings.SicknessAccidentInsuranceRate,
            settings.TrainingAndHolidayRate,
            settings.VacationCompensationRate,
            settings.VehiclePauschalzone1RateChf,
            settings.VehiclePauschalzone2RateChf,
            settings.VehicleRegiezone1RateChf);
    }
}
