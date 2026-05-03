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
        Assert.Equal(global::Payroll.Domain.Settings.PayrollSettings.DefaultSalaryCertificatePdfTemplatePath, loaded.SalaryCertificatePdfTemplatePath);
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

        await repository.SaveAsync(CreateCommand(), CancellationToken.None);
        var loaded = await repository.GetAsync(CancellationToken.None);

        Assert.Contains("Blesinger Sicherheits Dienste", loaded.CompanyAddress, StringComparison.Ordinal);
        Assert.Equal("Aptos", loaded.AppFontFamily);
        Assert.Equal(14m, loaded.AppFontSize);
        Assert.Equal("#FF224466", loaded.AppAccentColorHex);
        Assert.Equal("Helvetica", loaded.PrintFontFamily);
        Assert.Equal(10m, loaded.PrintFontSize);
        Assert.Equal("BANNER|Lohnblatt|{{Monat}}", loaded.PrintTemplate);
        Assert.Equal(global::Payroll.Domain.Settings.PayrollSettings.DefaultSalaryCertificatePdfTemplatePath, loaded.SalaryCertificatePdfTemplatePath);
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
        Assert.Equal(10, loaded.PayrollPreviewHelpOptions.Count);
        Assert.All(loaded.PayrollPreviewHelpOptions, option => Assert.True(option.IsEnabled));
        Assert.Equal(2, loaded.Departments.Count);
        Assert.Equal(2, loaded.EmploymentCategories.Count);
        Assert.Single(loaded.EmploymentLocations);
        Assert.NotNull(loaded.CurrentGeneralSettingsVersionId);
        Assert.NotNull(loaded.CurrentHourlySettingsVersionId);
        Assert.NotNull(loaded.CurrentMonthlySalarySettingsVersionId);
        Assert.Single(loaded.GeneralSettingsHistory!);
        Assert.Single(loaded.HourlySettingsHistory!);
        Assert.Single(loaded.MonthlySalarySettingsHistory!);
    }

    [Fact]
    public async Task SaveAndLoadAsync_PersistsAppTableCellVerticalPadding()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);

        await repository.SaveAsync(CreateCommand(), CancellationToken.None);
        var loaded = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(4m, loaded.AppTableCellVerticalPadding);
    }

    [Fact]
    public async Task SaveAndLoadAsync_PersistsDepartmentGavMandatoryFlag()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);

        var gavDepartmentId = Guid.NewGuid();
        await repository.SaveAsync(CreateCommand(
            departments: [new SettingOptionDto(gavDepartmentId, "Sicherheit", true), new SettingOptionDto(Guid.NewGuid(), "Buero", false)]),
            CancellationToken.None);
        var loaded = await repository.GetAsync(CancellationToken.None);

        Assert.Contains(loaded.Departments, item => item.OptionId == gavDepartmentId && item.IsGavMandatory);
        Assert.Contains(loaded.Departments, item => item.Name == "Buero" && !item.IsGavMandatory);
    }

    [Fact]
    public async Task SaveAndLoadAsync_PersistsCustomSalaryCertificatePdfTemplatePath()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);

        await repository.SaveAsync(CreateCommand(
            salaryCertificatePdfTemplatePath: "/tmp/custom-lohnausweis.pdf"),
            CancellationToken.None);
        var loaded = await repository.GetAsync(CancellationToken.None);

        Assert.Equal("/tmp/custom-lohnausweis.pdf", loaded.SalaryCertificatePdfTemplatePath);
    }

    [Fact]
    public async Task SaveAsync_WithExistingGeneralVersion_UpdatesCurrentStand()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);
        var initial = await repository.SaveAsync(CreateCommand(), CancellationToken.None);

        var saved = await repository.SaveAsync(CreateCommand(
            editingGeneralSettingsVersionId: initial.CurrentGeneralSettingsVersionId,
            generalSettingsValidFrom: new DateOnly(2026, 1, 1),
            ahvIvEoRate: 0.054m), CancellationToken.None);

        Assert.Equal(initial.CurrentGeneralSettingsVersionId, saved.CurrentGeneralSettingsVersionId);
        Assert.Equal(0.054m, saved.AhvIvEoRate);
        Assert.Single(saved.GeneralSettingsHistory!);
    }

    [Fact]
    public async Task SaveAsync_WithNullGeneralEditingId_CreatesNewGeneralVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);
        var initial = await repository.SaveAsync(CreateCommand(), CancellationToken.None);

        var saved = await repository.SaveAsync(CreateCommand(
            editingGeneralSettingsVersionId: null,
            generalSettingsValidFrom: new DateOnly(2026, 2, 1),
            ahvIvEoRate: 0.054m,
            currentHourlySettingsVersionId: initial.CurrentHourlySettingsVersionId,
            currentMonthlySalarySettingsVersionId: initial.CurrentMonthlySalarySettingsVersionId), CancellationToken.None);

        Assert.Equal(2, saved.GeneralSettingsHistory!.Count);
        Assert.NotEqual(initial.CurrentGeneralSettingsVersionId, saved.CurrentGeneralSettingsVersionId);
        Assert.Contains(saved.GeneralSettingsHistory, item => item.VersionId == initial.CurrentGeneralSettingsVersionId && item.ValidTo == new DateOnly(2026, 1, 31));
        Assert.Contains(saved.GeneralSettingsHistory, item => item.ValidFrom == new DateOnly(2026, 2, 1) && item.AhvIvEoRate == 0.054m);
    }

    [Fact]
    public async Task SaveAsync_PreventsOverlappingHourlyVersions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);
        var initial = await repository.SaveAsync(CreateCommand(), CancellationToken.None);
        var second = await repository.SaveAsync(CreateCommand(
            editingHourlySettingsVersionId: null,
            hourlySettingsValidFrom: new DateOnly(2026, 3, 1),
            nightSupplementRate: 0.30m,
            currentGeneralSettingsVersionId: initial.CurrentGeneralSettingsVersionId,
            currentMonthlySalarySettingsVersionId: initial.CurrentMonthlySalarySettingsVersionId), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.SaveAsync(CreateCommand(
                editingHourlySettingsVersionId: second.CurrentHourlySettingsVersionId,
                hourlySettingsValidFrom: new DateOnly(2026, 2, 1),
                currentGeneralSettingsVersionId: second.CurrentGeneralSettingsVersionId,
                currentMonthlySalarySettingsVersionId: second.CurrentMonthlySalarySettingsVersionId),
                CancellationToken.None));

        Assert.Contains("ueberschneiden", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_RetroactiveHourlyVersion_IsLimitedBeforeExistingLaterVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);
        _ = await repository.GetAsync(CancellationToken.None);

        var saved = await repository.SaveAsync(CreateCommand(
            editingHourlySettingsVersionId: null,
            hourlySettingsValidFrom: new DateOnly(2026, 1, 1),
            nightSupplementRate: 0.25m), CancellationToken.None);

        Assert.Contains(saved.HourlySettingsHistory!, item => item.ValidFrom == new DateOnly(2026, 1, 1) && item.ValidTo == new DateOnly(2026, 3, 31));
        Assert.Contains(saved.HourlySettingsHistory!, item => item.ValidFrom == new DateOnly(2026, 4, 1) && item.ValidTo is null);
    }

    [Fact]
    public async Task SaveAsync_MonthlySalaryArea_IsPreparedForHistory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);
        var initial = await repository.SaveAsync(CreateCommand(), CancellationToken.None);

        var saved = await repository.SaveAsync(CreateCommand(
            editingMonthlySalarySettingsVersionId: null,
            monthlySalarySettingsValidFrom: new DateOnly(2026, 4, 1),
            currentGeneralSettingsVersionId: initial.CurrentGeneralSettingsVersionId,
            currentHourlySettingsVersionId: initial.CurrentHourlySettingsVersionId), CancellationToken.None);

        Assert.Equal(2, saved.MonthlySalarySettingsHistory!.Count);
        Assert.Contains(saved.MonthlySalarySettingsHistory, item => item.ValidFrom == new DateOnly(2026, 4, 1));
    }

    private static SavePayrollSettingsCommand CreateCommand(
        Guid? editingGeneralSettingsVersionId = null,
        DateOnly? generalSettingsValidFrom = null,
        decimal ahvIvEoRate = 0.053m,
        Guid? currentGeneralSettingsVersionId = null,
        Guid? editingHourlySettingsVersionId = null,
        DateOnly? hourlySettingsValidFrom = null,
        decimal? nightSupplementRate = 0.25m,
        Guid? currentHourlySettingsVersionId = null,
        Guid? editingMonthlySalarySettingsVersionId = null,
        DateOnly? monthlySalarySettingsValidFrom = null,
        Guid? currentMonthlySalarySettingsVersionId = null,
        IReadOnlyCollection<SettingOptionDto>? departments = null,
        string salaryCertificatePdfTemplatePath = global::Payroll.Domain.Settings.PayrollSettings.DefaultSalaryCertificatePdfTemplatePath)
    {
        return new SavePayrollSettingsCommand(
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
            salaryCertificatePdfTemplatePath,
            ".",
            " ",
            "EUR",
            nightSupplementRate,
            0.50m,
            1.00m,
            ahvIvEoRate,
            0.011m,
            0.00821m,
            0.00015m,
            0.1064m,
            0.1264m,
            1.10m,
            2.20m,
            3.30m,
            PayrollPreviewHelpCatalog.GetDefaultOptions(),
            departments ?? [new SettingOptionDto(Guid.NewGuid(), "Sicherheit"), new SettingOptionDto(Guid.NewGuid(), "Buero")],
            [new SettingOptionDto(Guid.NewGuid(), "A"), new SettingOptionDto(Guid.NewGuid(), "B")],
            [new SettingOptionDto(Guid.NewGuid(), "Schachenstr. 7, Emmenbruecke")],
            editingGeneralSettingsVersionId ?? currentGeneralSettingsVersionId,
            generalSettingsValidFrom ?? new DateOnly(2026, 1, 1),
            null,
            editingHourlySettingsVersionId ?? currentHourlySettingsVersionId,
            hourlySettingsValidFrom ?? new DateOnly(2026, 1, 1),
            null,
            editingMonthlySalarySettingsVersionId ?? currentMonthlySalarySettingsVersionId,
            monthlySalarySettingsValidFrom ?? new DateOnly(2026, 1, 1),
            null,
            20m,
            12m,
            12m,
            8m,
            4m);
    }
}
