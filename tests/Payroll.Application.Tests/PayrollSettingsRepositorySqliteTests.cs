using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Settings;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Settings;

namespace Payroll.Application.Tests;

public sealed class PayrollSettingsRepositorySqliteTests
{
    [Fact]
    public async Task GetAsync_WhenNoTemplateWasSaved_ReturnsDefaultPrintTemplate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);

        var loaded = await repository.GetAsync(CancellationToken.None);

        Assert.Contains("PAYROLL_LINE|regular|Basislohn", loaded.PrintTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAndLoadAsync_PersistsCentralSupplementRates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);

        await repository.SaveAsync(new SavePayrollSettingsCommand(
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
            ".",
            " ",
            "EUR",
            null,
            new DateOnly(2026, 4, 1),
            null,
            0.25m, 0.50m, 1.00m, 0.053m, 0.011m, 0.00821m, 0.00015m, 0.1064m, 0.1264m, 1.10m, 2.20m, 3.30m,
            PayrollPreviewHelpCatalog.GetDefaultOptions(),
            [new SettingOptionDto(Guid.NewGuid(), "Sicherheit"), new SettingOptionDto(Guid.NewGuid(), "Buero")],
            [new SettingOptionDto(Guid.NewGuid(), "A"), new SettingOptionDto(Guid.NewGuid(), "B")],
            [new SettingOptionDto(Guid.NewGuid(), "Schachenstr. 7, Emmenbruecke")]),
            CancellationToken.None);
        var loaded = await repository.GetAsync(CancellationToken.None);

        Assert.Contains("Blesinger Sicherheits Dienste", loaded.CompanyAddress, StringComparison.Ordinal);
        Assert.Equal("Aptos", loaded.AppFontFamily);
        Assert.Equal(14m, loaded.AppFontSize);
        Assert.Equal("#FF224466", loaded.AppAccentColorHex);
        Assert.Equal("Helvetica", loaded.PrintFontFamily);
        Assert.Equal(10m, loaded.PrintFontSize);
        Assert.Equal("BANNER|Lohnblatt|{{Monat}}", loaded.PrintTemplate);
        Assert.Equal(".", loaded.DecimalSeparator);
        Assert.Equal(" ", loaded.ThousandsSeparator);
        Assert.Equal("EUR", loaded.CurrencyCode);
        Assert.Equal(0.25m, loaded.NightSupplementRate);
        Assert.Equal(0.50m, loaded.SundaySupplementRate);
        Assert.Equal(1.00m, loaded.HolidaySupplementRate);
        Assert.Equal(0.053m, loaded.AhvIvEoRate);
        Assert.Equal(0.011m, loaded.AlvRate);
        Assert.Equal(0.00821m, loaded.SicknessAccidentInsuranceRate);
        Assert.Equal(0.00015m, loaded.TrainingAndHolidayRate);
        Assert.Equal(0.1064m, loaded.VacationCompensationRate);
        Assert.Equal(0.1264m, loaded.VacationCompensationRateAge50Plus);
        Assert.Equal(1.10m, loaded.VehiclePauschalzone1RateChf);
        Assert.Equal(2.20m, loaded.VehiclePauschalzone2RateChf);
        Assert.Equal(3.30m, loaded.VehicleRegiezone1RateChf);
        Assert.Equal(8, loaded.PayrollPreviewHelpOptions.Count);
        Assert.All(loaded.PayrollPreviewHelpOptions, option => Assert.True(option.IsEnabled));
        Assert.Equal(2, loaded.Departments.Count);
        Assert.Equal(2, loaded.EmploymentCategories.Count);
        Assert.Single(loaded.EmploymentLocations);
    }

    [Fact]
    public async Task DeleteCalculationVersionAsync_RejectsActiveVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);
        var loaded = await repository.GetAsync(CancellationToken.None);
        var activeVersion = Assert.Single(loaded.CalculationVersions);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.DeleteCalculationVersionAsync(activeVersion.VersionId, CancellationToken.None));

        Assert.Contains("aktive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WhenEditingCurrentVersion_UpdatesExistingVersionInsteadOfCreatingNewOne()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);
        var loaded = await repository.GetAsync(CancellationToken.None);
        var currentVersion = Assert.Single(loaded.CalculationVersions);

        var saved = await repository.SaveAsync(new SavePayrollSettingsCommand(
            loaded.CompanyAddress,
            loaded.AppFontFamily,
            loaded.AppFontSize,
            loaded.AppTextColorHex,
            loaded.AppMutedTextColorHex,
            loaded.AppBackgroundColorHex,
            loaded.AppAccentColorHex,
            loaded.AppLogoText,
            loaded.AppLogoPath,
            loaded.PrintFontFamily,
            loaded.PrintFontSize,
            loaded.PrintTextColorHex,
            loaded.PrintMutedTextColorHex,
            loaded.PrintAccentColorHex,
            loaded.PrintLogoText,
            loaded.PrintLogoPath,
            loaded.PrintTemplate,
            loaded.DecimalSeparator,
            loaded.ThousandsSeparator,
            loaded.CurrencyCode,
            currentVersion.VersionId,
            new DateOnly(2025, 12, 1),
            null,
            loaded.NightSupplementRate,
            loaded.SundaySupplementRate,
            loaded.HolidaySupplementRate,
            loaded.AhvIvEoRate,
            loaded.AlvRate,
            loaded.SicknessAccidentInsuranceRate,
            loaded.TrainingAndHolidayRate,
            loaded.VacationCompensationRate,
            loaded.VacationCompensationRateAge50Plus,
            loaded.VehiclePauschalzone1RateChf,
            loaded.VehiclePauschalzone2RateChf,
            loaded.VehicleRegiezone1RateChf,
            loaded.PayrollPreviewHelpOptions,
            loaded.Departments,
            loaded.EmploymentCategories,
            loaded.EmploymentLocations),
            CancellationToken.None);

        Assert.Single(saved.CalculationVersions);
        Assert.Equal(new DateOnly(2025, 12, 1), saved.CalculationValidFrom);
        Assert.Equal(new DateOnly(2025, 12, 1), Assert.Single(saved.CalculationVersions).ValidFrom);
    }

    [Fact]
    public async Task SaveAsync_WhenCreatingNewVersion_KeepsExistingVersionAndAddsNewOne()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);
        var loaded = await repository.GetAsync(CancellationToken.None);
        var originalVersion = Assert.Single(loaded.CalculationVersions);

        var saved = await repository.SaveAsync(new SavePayrollSettingsCommand(
            loaded.CompanyAddress,
            loaded.AppFontFamily,
            loaded.AppFontSize,
            loaded.AppTextColorHex,
            loaded.AppMutedTextColorHex,
            loaded.AppBackgroundColorHex,
            loaded.AppAccentColorHex,
            loaded.AppLogoText,
            loaded.AppLogoPath,
            loaded.PrintFontFamily,
            loaded.PrintFontSize,
            loaded.PrintTextColorHex,
            loaded.PrintMutedTextColorHex,
            loaded.PrintAccentColorHex,
            loaded.PrintLogoText,
            loaded.PrintLogoPath,
            loaded.PrintTemplate,
            loaded.DecimalSeparator,
            loaded.ThousandsSeparator,
            loaded.CurrencyCode,
            null,
            new DateOnly(2026, 2, 1),
            null,
            0.30m,
            loaded.SundaySupplementRate,
            loaded.HolidaySupplementRate,
            loaded.AhvIvEoRate,
            loaded.AlvRate,
            loaded.SicknessAccidentInsuranceRate,
            loaded.TrainingAndHolidayRate,
            loaded.VacationCompensationRate,
            loaded.VacationCompensationRateAge50Plus,
            loaded.VehiclePauschalzone1RateChf,
            loaded.VehiclePauschalzone2RateChf,
            loaded.VehicleRegiezone1RateChf,
            loaded.PayrollPreviewHelpOptions,
            loaded.Departments,
            loaded.EmploymentCategories,
            loaded.EmploymentLocations),
            CancellationToken.None);

        Assert.Equal(2, saved.CalculationVersions.Count);
        Assert.Contains(saved.CalculationVersions, item => item.VersionId == originalVersion.VersionId && item.ValidTo == new DateOnly(2026, 1, 31));
        Assert.Contains(saved.CalculationVersions, item => item.ValidFrom == new DateOnly(2026, 2, 1) && item.ValidTo is null);
        Assert.Equal(0.30m, saved.NightSupplementRate);
    }
}
