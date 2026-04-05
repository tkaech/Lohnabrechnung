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

        var saved = await service.SaveAsync(new SavePayrollSettingsCommand(0.25m, 0.50m, 1.00m));
        var loadedSettings = await service.GetWorkTimeSupplementSettingsAsync();

        Assert.Equal(0.25m, saved.NightSupplementRate);
        Assert.Equal(0.50m, saved.SundaySupplementRate);
        Assert.Equal(1.00m, saved.HolidaySupplementRate);
        Assert.Equal(0.25m, loadedSettings.NightSupplementRate);
        Assert.Equal(0.50m, loadedSettings.SundaySupplementRate);
        Assert.Equal(1.00m, loadedSettings.HolidaySupplementRate);
    }

    private sealed class InMemoryPayrollSettingsRepository : IPayrollSettingsRepository
    {
        private PayrollSettingsDto _settings = new(null, null, null);

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
            _settings = new PayrollSettingsDto(command.NightSupplementRate, command.SundaySupplementRate, command.HolidaySupplementRate);
            return Task.FromResult(_settings);
        }
    }
}
