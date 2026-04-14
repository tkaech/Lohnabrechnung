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

        var saved = await service.SaveAsync(new SavePayrollSettingsCommand(
            "Blesinger Sicherheits Dienste GmbH\nPostfach 28\n6314 Unteraegeri",
            "Aptos",
            14m,
            "#FF101820",
            "#FF667788",
            "#FFF6F8FB",
            "#FF224466",
            "BSD",
            "/tmp/app-logo.png",
            "Helvetica",
            10m,
            "#FF000000",
            "#FF556677",
            "#FFFFFF00",
            "BSD",
            "/tmp/print-logo.png",
            "BANNER|Lohnblatt|{{Monat}}",
            ",",
            "'",
            "CHF",
            null,
            new DateOnly(2026, 4, 1),
            null,
            0.25m, 0.50m, 1.00m, 0.053m, 0.011m, 0.00821m, 0.00015m, 0.1064m, 0.1264m, 1.10m, 2.20m, 3.30m,
            PayrollPreviewHelpCatalog.GetDefaultOptions(),
            [new SettingOptionDto(Guid.NewGuid(), "Sicherheit")],
            [new SettingOptionDto(Guid.NewGuid(), "A")],
            [new SettingOptionDto(Guid.NewGuid(), "Schachenstr. 7, Emmenbruecke")]));
        var loadedSettings = await service.GetWorkTimeSupplementSettingsAsync();

        Assert.Contains("Blesinger Sicherheits Dienste", saved.CompanyAddress, StringComparison.Ordinal);
        Assert.Equal("Aptos", saved.AppFontFamily);
        Assert.Equal(14m, saved.AppFontSize);
        Assert.Equal("#FF224466", saved.AppAccentColorHex);
        Assert.Equal("Helvetica", saved.PrintFontFamily);
        Assert.Equal(10m, saved.PrintFontSize);
        Assert.Equal("BSD", saved.PrintLogoText);
        Assert.Equal("BANNER|Lohnblatt|{{Monat}}", saved.PrintTemplate);
        Assert.Equal(",", saved.DecimalSeparator);
        Assert.Equal("'", saved.ThousandsSeparator);
        Assert.Equal("CHF", saved.CurrencyCode);
        Assert.Equal(0.25m, saved.NightSupplementRate);
        Assert.Equal(0.50m, saved.SundaySupplementRate);
        Assert.Equal(1.00m, saved.HolidaySupplementRate);
        Assert.Equal(0.053m, saved.AhvIvEoRate);
        Assert.Equal(0.011m, saved.AlvRate);
        Assert.Equal(0.00821m, saved.SicknessAccidentInsuranceRate);
        Assert.Equal(0.00015m, saved.TrainingAndHolidayRate);
        Assert.Equal(0.1064m, saved.VacationCompensationRate);
        Assert.Equal(0.1264m, saved.VacationCompensationRateAge50Plus);
        Assert.Equal(1.10m, saved.VehiclePauschalzone1RateChf);
        Assert.Equal(2.20m, saved.VehiclePauschalzone2RateChf);
        Assert.Equal(3.30m, saved.VehicleRegiezone1RateChf);
        Assert.Equal(8, saved.PayrollPreviewHelpOptions.Count);
        Assert.Single(saved.Departments);
        Assert.Single(saved.EmploymentCategories);
        Assert.Single(saved.EmploymentLocations);
        Assert.Equal(0.25m, loadedSettings.NightSupplementRate);
        Assert.Equal(0.50m, loadedSettings.SundaySupplementRate);
        Assert.Equal(1.00m, loadedSettings.HolidaySupplementRate);
    }

    private sealed class InMemoryPayrollSettingsRepository : IPayrollSettingsRepository
    {
        private PayrollSettingsDto _settings = new(
            string.Empty,
            "Segoe UI",
            13m,
            "#FF1A2530",
            "#FF5F6B7A",
            "#FFF5F7FA",
            "#FF14324A",
            "PA",
            string.Empty,
            "Helvetica",
            9m,
            "#FF000000",
            "#FF4B5563",
            "#FFFFFF00",
            "PA",
            string.Empty,
            string.Empty,
            global::Payroll.Domain.Settings.PayrollSettings.DefaultDecimalSeparator,
            global::Payroll.Domain.Settings.PayrollSettings.DefaultThousandsSeparator,
            global::Payroll.Domain.Settings.PayrollSettings.DefaultCurrencyCode,
            new DateOnly(2026, 4, 1),
            null,
            null, null, null, 0.053m, 0.011m, 0.00821m, 0.00015m, 0.1064m, 0.1264m, 0m, 0m, 0m,
            [new PayrollCalculationSettingsVersionDto(Guid.NewGuid(), new DateOnly(2026, 4, 1), null, null, null, null, 0.053m, 0.011m, 0.00821m, 0.00015m, 0.1064m, 0.1264m, 0m, 0m, 0m, true)],
            PayrollPreviewHelpCatalog.GetDefaultOptions(), [], [], []);

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
                command.CompanyAddress,
                command.AppFontFamily,
                command.AppFontSize,
                command.AppTextColorHex,
                command.AppMutedTextColorHex,
                command.AppBackgroundColorHex,
                command.AppAccentColorHex,
                command.AppLogoText,
                command.AppLogoPath,
                command.PrintFontFamily,
                command.PrintFontSize,
                command.PrintTextColorHex,
                command.PrintMutedTextColorHex,
                command.PrintAccentColorHex,
                command.PrintLogoText,
                command.PrintLogoPath,
                command.PrintTemplate,
                command.DecimalSeparator,
                command.ThousandsSeparator,
                command.CurrencyCode,
                command.CalculationValidFrom,
                command.CalculationValidTo,
                command.NightSupplementRate,
                command.SundaySupplementRate,
                command.HolidaySupplementRate,
                command.AhvIvEoRate,
                command.AlvRate,
                command.SicknessAccidentInsuranceRate,
                command.TrainingAndHolidayRate,
                command.VacationCompensationRate,
                command.VacationCompensationRateAge50Plus,
                command.VehiclePauschalzone1RateChf,
                command.VehiclePauschalzone2RateChf,
                command.VehicleRegiezone1RateChf,
                [new PayrollCalculationSettingsVersionDto(Guid.NewGuid(), command.CalculationValidFrom, command.CalculationValidTo, command.NightSupplementRate, command.SundaySupplementRate, command.HolidaySupplementRate, command.AhvIvEoRate, command.AlvRate, command.SicknessAccidentInsuranceRate, command.TrainingAndHolidayRate, command.VacationCompensationRate, command.VacationCompensationRateAge50Plus, command.VehiclePauschalzone1RateChf, command.VehiclePauschalzone2RateChf, command.VehicleRegiezone1RateChf, true)],
                command.PayrollPreviewHelpOptions,
                command.Departments,
                command.EmploymentCategories,
                command.EmploymentLocations);
            return Task.FromResult(_settings);
        }

        public Task<PayrollSettingsDto> DeleteCalculationVersionAsync(Guid versionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
