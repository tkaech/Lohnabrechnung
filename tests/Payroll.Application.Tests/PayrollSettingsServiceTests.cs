using Payroll.Application.Settings;
using Payroll.Domain.Employees;

namespace Payroll.Application.Tests;

public sealed class PayrollSettingsServiceTests
{
    [Fact]
    public async Task SaveAsync_StoresCentralSupplementRates()
    {
        var repository = new InMemoryPayrollSettingsRepository();
        var service = new PayrollSettingsService(repository);

        var saved = await service.SaveAsync(new SavePayrollSettingsCommand(0.25m, 0.50m, 1.00m, 0.053m, 0.011m, 0.00821m, 0.00015m, 0.1064m, 1.10m, 2.20m, 3.30m));
        var loadedSettings = await service.GetWorkTimeSupplementSettingsAsync();

        Assert.Equal(0.25m, saved.NightSupplementRate);
        Assert.Equal(0.50m, saved.SundaySupplementRate);
        Assert.Equal(1.00m, saved.HolidaySupplementRate);
        Assert.Equal(0.053m, saved.AhvIvEoRate);
        Assert.Equal(0.011m, saved.AlvRate);
        Assert.Equal(0.00821m, saved.SicknessAccidentInsuranceRate);
        Assert.Equal(0.00015m, saved.TrainingAndHolidayRate);
        Assert.Equal(0.1064m, saved.VacationCompensationRate);
        Assert.Equal(1.10m, saved.VehiclePauschalzone1RateChf);
        Assert.Equal(2.20m, saved.VehiclePauschalzone2RateChf);
        Assert.Equal(3.30m, saved.VehicleRegiezone1RateChf);
        Assert.Equal(0.25m, loadedSettings.NightSupplementRate);
        Assert.Equal(0.50m, loadedSettings.SundaySupplementRate);
        Assert.Equal(1.00m, loadedSettings.HolidaySupplementRate);
    }

    private sealed class InMemoryPayrollSettingsRepository : IPayrollSettingsRepository
    {
        private PayrollSettingsDto _settings = new(null, null, null, 0.053m, 0.011m, 0.00821m, 0.00015m, 0.1064m, 0m, 0m, 0m);

        public Task<PayrollSettingsDto> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_settings);
        }

        public Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new WorkTimeSupplementSettings(
                _settings.NightSupplementRate,
                _settings.SundaySupplementRate,
                _settings.HolidaySupplementRate));
        }

        public Task<PayrollSettingsDto> SaveAsync(SavePayrollSettingsCommand command, CancellationToken cancellationToken)
        {
            _settings = new PayrollSettingsDto(
                command.NightSupplementRate,
                command.SundaySupplementRate,
                command.HolidaySupplementRate,
                command.AhvIvEoRate,
                command.AlvRate,
                command.SicknessAccidentInsuranceRate,
                command.TrainingAndHolidayRate,
                command.VacationCompensationRate,
                command.VehiclePauschalzone1RateChf,
                command.VehiclePauschalzone2RateChf,
                command.VehicleRegiezone1RateChf);
            return Task.FromResult(_settings);
        }
    }
}
