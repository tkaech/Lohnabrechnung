using Microsoft.EntityFrameworkCore;
using Payroll.Application.Reporting;
using Payroll.Application.Settings;
using Payroll.Domain.Employees;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.Persistence;
using System.Text.Json;

namespace Payroll.Infrastructure.Settings;

public sealed class PayrollSettingsRepository : IPayrollSettingsRepository
{
    private static readonly string[] DefaultDepartments = ["Sicherheit", "Buero"];
    private static readonly string[] DefaultEmploymentCategories = ["A", "B", "C"];
    private static readonly string[] DefaultEmploymentLocations =
    [
        "Schachenstr. 7, Emmenbruecke",
        "Weinbergstrasse 8, Baar",
        "Rainstrasse 37, Unteraegeri"
    ];
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly PayrollDbContext _dbContext;

    public PayrollSettingsRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PayrollSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        var calculationVersion = await LoadLatestCalculationSettingsVersionAsync(cancellationToken);
        return await ToDto(settings, calculationVersion, cancellationToken);
    }

    public async Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken)
    {
        var version = await LoadCurrentCalculationSettingsVersionAsync(DateOnly.FromDateTime(DateTime.Today), cancellationToken);
        if (version is not null)
        {
            return version.WorkTimeSupplementSettings;
        }

        var settings = await GetOrCreateAsync(cancellationToken);
        return settings.WorkTimeSupplementSettings;
    }

    public async Task<PayrollSettingsDto> SaveAsync(SavePayrollSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        settings.UpdateCompanyAddress(command.CompanyAddress);
        settings.UpdateVisualSettings(
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
            command.PrintLogoPath);
        settings.UpdatePrintTemplate(command.PrintTemplate);
        settings.UpdateDecimalSeparator(command.DecimalSeparator);
        settings.UpdateThousandsSeparator(command.ThousandsSeparator);
        settings.UpdateCurrencyCode(command.CurrencyCode);
        settings.UpdatePayrollPreviewHelpVisibilityJson(SerializePayrollPreviewHelpOptions(command.PayrollPreviewHelpOptions));
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
            command.VacationCompensationRateAge50Plus,
            command.VehiclePauschalzone1RateChf,
            command.VehiclePauschalzone2RateChf,
            command.VehicleRegiezone1RateChf);
        var calculationVersion = await UpsertCalculationVersionAsync(command, cancellationToken);
        await SyncOptionsAsync(_dbContext.DepartmentOptions, command.Departments, nameof(global::Payroll.Domain.Employees.Employee.DepartmentOptionId), "Abteilung", cancellationToken);
        await SyncOptionsAsync(_dbContext.EmploymentCategoryOptions, command.EmploymentCategories, nameof(global::Payroll.Domain.Employees.Employee.EmploymentCategoryOptionId), "Anstellungskategorie", cancellationToken);
        await SyncOptionsAsync(_dbContext.EmploymentLocationOptions, command.EmploymentLocations, nameof(global::Payroll.Domain.Employees.Employee.EmploymentLocationOptionId), "Anstellungsort", cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await ToDto(settings, calculationVersion, cancellationToken);
    }

    public async Task<PayrollSettingsDto> DeleteCalculationVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        var versions = await _dbContext.PayrollCalculationSettingsVersions
            .OrderBy(item => item.ValidFrom)
            .ToListAsync(cancellationToken);

        var version = versions.SingleOrDefault(item => item.Id == versionId)
            ?? throw new InvalidOperationException("Satzstand wurde nicht gefunden.");

        var currentVersionId = versions
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .First()
            .Id;

        if (version.Id == currentVersionId)
        {
            throw new InvalidOperationException("Der aktive Satzstand kann nicht geloescht werden.");
        }

        var previousVersion = versions
            .Where(item => item.ValidFrom < version.ValidFrom)
            .OrderByDescending(item => item.ValidFrom)
            .FirstOrDefault();
        var nextVersion = versions
            .Where(item => item.ValidFrom > version.ValidFrom)
            .OrderBy(item => item.ValidFrom)
            .FirstOrDefault();

        _dbContext.PayrollCalculationSettingsVersions.Remove(version);

        if (previousVersion is not null)
        {
            var newValidTo = nextVersion is not null
                ? nextVersion.ValidFrom.AddDays(-1)
                : (DateOnly?)null;
            previousVersion.UpdatePeriod(previousVersion.ValidFrom, newValidTo);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var currentVersion = await LoadLatestCalculationSettingsVersionAsync(cancellationToken);
        return await ToDto(settings, currentVersion, cancellationToken);
    }

    private async Task<PayrollSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.Set<PayrollSettings>().SingleOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            await EnsureDefaultOptionsAsync(cancellationToken);
            if (!await _dbContext.PayrollCalculationSettingsVersions.AnyAsync(cancellationToken))
            {
                _dbContext.PayrollCalculationSettingsVersions.Add(PayrollCalculationSettingsVersion.Create(
                    NormalizeToMonthStart(DateOnly.FromDateTime(DateTime.Today)),
                    null,
                    settings));
            }

            if (_dbContext.ChangeTracker.HasChanges())
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return settings;
        }

        settings = new PayrollSettings();
        _dbContext.Set<PayrollSettings>().Add(settings);
        await EnsureDefaultOptionsAsync(cancellationToken);
        if (!await _dbContext.PayrollCalculationSettingsVersions.AnyAsync(cancellationToken))
        {
            _dbContext.PayrollCalculationSettingsVersions.Add(PayrollCalculationSettingsVersion.Create(
                NormalizeToMonthStart(DateOnly.FromDateTime(DateTime.Today)),
                null,
                settings));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private async Task EnsureDefaultOptionsAsync(CancellationToken cancellationToken)
    {
        await EnsureDefaultOptionsAsync(_dbContext.DepartmentOptions, DefaultDepartments, cancellationToken);
        await EnsureDefaultOptionsAsync(_dbContext.EmploymentCategoryOptions, DefaultEmploymentCategories, cancellationToken);
        await EnsureDefaultOptionsAsync(_dbContext.EmploymentLocationOptions, DefaultEmploymentLocations, cancellationToken);
    }

    private static async Task EnsureDefaultOptionsAsync<TOption>(
        DbSet<TOption> options,
        IReadOnlyCollection<string> defaultNames,
        CancellationToken cancellationToken)
        where TOption : class
    {
        var existingNames = await options
            .AsNoTracking()
            .Select(item => EF.Property<string>(item, "Name"))
            .ToListAsync(cancellationToken);

        if (existingNames.Count > 0)
        {
            return;
        }

        foreach (var defaultName in defaultNames)
        {
            var entity = (TOption)Activator.CreateInstance(typeof(TOption), defaultName)!;
            options.Add(entity);
        }
    }

    private async Task<PayrollSettingsDto> ToDto(
        PayrollSettings settings,
        PayrollCalculationSettingsVersion? calculationVersion,
        CancellationToken cancellationToken)
    {
        var printTemplate = string.IsNullOrWhiteSpace(settings.PrintTemplate)
            ? PayrollStatementTemplateProvider.LoadDefaultTemplate()
            : settings.PrintTemplate;
        var effectiveCalculationSettings = calculationVersion?.ToPayrollSettings() ?? settings;
        var calculationVersions = await LoadCalculationVersionsAsync(cancellationToken);

        return new PayrollSettingsDto(
            settings.CompanyAddress,
            settings.AppFontFamily,
            settings.AppFontSize,
            settings.AppTextColorHex,
            settings.AppMutedTextColorHex,
            settings.AppBackgroundColorHex,
            settings.AppAccentColorHex,
            settings.AppLogoText,
            settings.AppLogoPath,
            settings.PrintFontFamily,
            settings.PrintFontSize,
            settings.PrintTextColorHex,
            settings.PrintMutedTextColorHex,
            settings.PrintAccentColorHex,
            settings.PrintLogoText,
            settings.PrintLogoPath,
            printTemplate,
            settings.DecimalSeparator,
            settings.ThousandsSeparator,
            settings.CurrencyCode,
            calculationVersion?.ValidFrom ?? NormalizeToMonthStart(DateOnly.FromDateTime(DateTime.Today)),
            calculationVersion?.ValidTo,
            effectiveCalculationSettings.WorkTimeSupplementSettings.NightSupplementRate,
            effectiveCalculationSettings.WorkTimeSupplementSettings.SundaySupplementRate,
            effectiveCalculationSettings.WorkTimeSupplementSettings.HolidaySupplementRate,
            effectiveCalculationSettings.AhvIvEoRate,
            effectiveCalculationSettings.AlvRate,
            effectiveCalculationSettings.SicknessAccidentInsuranceRate,
            effectiveCalculationSettings.TrainingAndHolidayRate,
            effectiveCalculationSettings.VacationCompensationRate,
            effectiveCalculationSettings.VacationCompensationRateAge50Plus,
            effectiveCalculationSettings.VehiclePauschalzone1RateChf,
            effectiveCalculationSettings.VehiclePauschalzone2RateChf,
            effectiveCalculationSettings.VehicleRegiezone1RateChf,
            calculationVersions,
            LoadPayrollPreviewHelpOptions(settings),
            await LoadOptionsAsync(_dbContext.DepartmentOptions, cancellationToken),
            await LoadOptionsAsync(_dbContext.EmploymentCategoryOptions, cancellationToken),
            await LoadOptionsAsync(_dbContext.EmploymentLocationOptions, cancellationToken));
    }

    private async Task<PayrollCalculationSettingsVersion?> LoadLatestCalculationSettingsVersionAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.PayrollCalculationSettingsVersions
            .AsNoTracking()
            .OrderByDescending(item => item.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<PayrollCalculationSettingsVersion?> LoadCurrentCalculationSettingsVersionAsync(DateOnly date, CancellationToken cancellationToken)
    {
        return await _dbContext.PayrollCalculationSettingsVersions
            .AsNoTracking()
            .Where(item => item.ValidFrom <= date
                && (!item.ValidTo.HasValue || item.ValidTo.Value >= date))
            .OrderByDescending(item => item.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<PayrollCalculationSettingsVersionDto>> LoadCalculationVersionsAsync(CancellationToken cancellationToken)
    {
        var versions = await _dbContext.PayrollCalculationSettingsVersions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        versions = versions
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .ToList();

        var currentVersionId = versions.FirstOrDefault()?.Id;

        return versions
            .Select(version => new PayrollCalculationSettingsVersionDto(
                version.Id,
                version.ValidFrom,
                version.ValidTo,
                version.WorkTimeSupplementSettings.NightSupplementRate,
                version.WorkTimeSupplementSettings.SundaySupplementRate,
                version.WorkTimeSupplementSettings.HolidaySupplementRate,
                version.AhvIvEoRate,
                version.AlvRate,
                version.SicknessAccidentInsuranceRate,
                version.TrainingAndHolidayRate,
                version.VacationCompensationRate,
                version.VacationCompensationRateAge50Plus,
                version.VehiclePauschalzone1RateChf,
                version.VehiclePauschalzone2RateChf,
                version.VehicleRegiezone1RateChf,
                version.Id == currentVersionId))
            .ToArray();
    }

    private async Task<PayrollCalculationSettingsVersion> UpsertCalculationVersionAsync(
        SavePayrollSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var validFrom = NormalizeToMonthStart(command.CalculationValidFrom);
        DateOnly? validTo = command.CalculationValidTo.HasValue
            ? NormalizeToMonthEnd(command.CalculationValidTo.Value)
            : null;

        var existingVersions = await _dbContext.PayrollCalculationSettingsVersions
            .OrderBy(item => item.ValidFrom)
            .ToListAsync(cancellationToken);

        var version = command.EditingCalculationVersionId.HasValue
            ? existingVersions.SingleOrDefault(item => item.Id == command.EditingCalculationVersionId.Value)
            : existingVersions.SingleOrDefault(item => item.ValidFrom == validFrom);
        if (command.EditingCalculationVersionId.HasValue && version is null)
        {
            throw new InvalidOperationException("Der zu bearbeitende Satzstand wurde nicht gefunden.");
        }

        if (existingVersions.Any(item => item.Id != version?.Id && item.ValidFrom == validFrom))
        {
            throw new InvalidOperationException("Es existiert bereits ein anderer Satzstand mit demselben Gueltig-ab-Datum.");
        }

        var nextVersion = existingVersions
            .Where(item => item.Id != version?.Id && item.ValidFrom > validFrom)
            .OrderBy(item => item.ValidFrom)
            .FirstOrDefault();

        if (!validTo.HasValue && nextVersion is not null)
        {
            validTo = nextVersion.ValidFrom.AddDays(-1);
        }

        if (validTo.HasValue && nextVersion is not null && nextVersion.ValidFrom <= validTo.Value)
        {
            throw new InvalidOperationException("Der Gueltigkeitsbereich der globalen Saetze ueberlappt mit einem spaeteren Satzstand.");
        }

        var previousVersion = existingVersions
            .Where(item => item.Id != version?.Id && item.ValidFrom < validFrom)
            .OrderByDescending(item => item.ValidFrom)
            .FirstOrDefault();

        if (previousVersion is not null && (!previousVersion.ValidTo.HasValue || previousVersion.ValidTo.Value >= validFrom))
        {
            previousVersion.UpdatePeriod(previousVersion.ValidFrom, validFrom.AddDays(-1));
        }

        var workTimeSupplementSettings = new WorkTimeSupplementSettings(
            command.NightSupplementRate,
            command.SundaySupplementRate,
            command.HolidaySupplementRate);

        if (version is null)
        {
            version = new PayrollCalculationSettingsVersion(
                validFrom,
                validTo,
                workTimeSupplementSettings,
                command.AhvIvEoRate,
                command.AlvRate,
                command.SicknessAccidentInsuranceRate,
                command.TrainingAndHolidayRate,
                command.VacationCompensationRate,
                command.VacationCompensationRateAge50Plus,
                command.VehiclePauschalzone1RateChf,
                command.VehiclePauschalzone2RateChf,
                command.VehicleRegiezone1RateChf);
            _dbContext.PayrollCalculationSettingsVersions.Add(version);
            return version;
        }

        version.UpdatePeriod(validFrom, validTo);
        version.UpdateRates(
            workTimeSupplementSettings,
            command.AhvIvEoRate,
            command.AlvRate,
            command.SicknessAccidentInsuranceRate,
            command.TrainingAndHolidayRate,
            command.VacationCompensationRate,
            command.VacationCompensationRateAge50Plus,
            command.VehiclePauschalzone1RateChf,
            command.VehiclePauschalzone2RateChf,
            command.VehicleRegiezone1RateChf);
        return version;
    }

    private static DateOnly NormalizeToMonthStart(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, 1);
    }

    private static DateOnly NormalizeToMonthEnd(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }

    private static IReadOnlyCollection<PayrollPreviewHelpOptionDto> LoadPayrollPreviewHelpOptions(PayrollSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PayrollPreviewHelpVisibilityJson))
        {
            return PayrollPreviewHelpCatalog.GetDefaultOptions();
        }

        try
        {
            var storedOptions = JsonSerializer.Deserialize<PayrollPreviewHelpVisibility[]>(
                settings.PayrollPreviewHelpVisibilityJson,
                JsonSerializerOptions);

            return PayrollPreviewHelpCatalog.MergeWithDefaults(storedOptions ?? []);
        }
        catch (JsonException)
        {
            return PayrollPreviewHelpCatalog.GetDefaultOptions();
        }
    }

    private static string SerializePayrollPreviewHelpOptions(IReadOnlyCollection<PayrollPreviewHelpOptionDto> options)
    {
        var normalized = PayrollPreviewHelpCatalog.ToDomain(options);
        return JsonSerializer.Serialize(normalized, JsonSerializerOptions);
    }

    private static Task<SettingOptionDto[]> LoadOptionsAsync<TOption>(DbSet<TOption> options, CancellationToken cancellationToken)
        where TOption : class
    {
        return options
            .AsNoTracking()
            .OrderBy(item => EF.Property<string>(item, "Name"))
            .Select(item => new SettingOptionDto(
                EF.Property<Guid>(item, "Id"),
                EF.Property<string>(item, "Name")))
            .ToArrayAsync(cancellationToken);
    }

    private async Task SyncOptionsAsync<TOption>(
        DbSet<TOption> options,
        IReadOnlyCollection<SettingOptionDto> submittedOptions,
        string employeeReferencePropertyName,
        string optionLabel,
        CancellationToken cancellationToken)
        where TOption : class
    {
        var normalizedOptions = submittedOptions
            .Select(item => new SettingOptionDto(
                item.OptionId == Guid.Empty ? Guid.NewGuid() : item.OptionId,
                item.Name.Trim()))
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        var existingOptions = await options.ToListAsync(cancellationToken);
        var submittedIds = normalizedOptions.Select(item => item.OptionId).ToHashSet();

        foreach (var submitted in normalizedOptions)
        {
            var existing = existingOptions.SingleOrDefault(item => GetOptionId(item) == submitted.OptionId);
            if (existing is null)
            {
                var created = (TOption)Activator.CreateInstance(typeof(TOption), submitted.Name)!;
                SetOptionId(created, submitted.OptionId);
                options.Add(created);
                continue;
            }

            RenameOption(existing, submitted.Name);
        }

        var removedIds = existingOptions
            .Select(GetOptionId)
            .Where(id => !submittedIds.Contains(id))
            .ToArray();

        if (removedIds.Length == 0)
        {
            return;
        }

        var referencedIds = await _dbContext.Employees
            .AsNoTracking()
            .Select(employee => EF.Property<Guid?>(employee, employeeReferencePropertyName))
            .Where(id => id.HasValue && removedIds.Contains(id.Value))
            .Select(id => id!.Value)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        if (referencedIds.Length > 0)
        {
            throw new InvalidOperationException($"{optionLabel}-Eintraege koennen nicht geloescht werden, solange sie Mitarbeitenden zugeordnet sind.");
        }

        foreach (var removable in existingOptions.Where(item => removedIds.Contains(GetOptionId(item))))
        {
            options.Remove(removable);
        }
    }

    private static Guid GetOptionId<TOption>(TOption option) where TOption : class
    {
        return (Guid)typeof(TOption).GetProperty("Id")!.GetValue(option)!;
    }

    private static void SetOptionId<TOption>(TOption option, Guid id) where TOption : class
    {
        typeof(TOption).GetProperty("Id")!.SetValue(option, id);
    }

    private static void RenameOption<TOption>(TOption option, string name) where TOption : class
    {
        typeof(TOption).GetMethod("Rename")!.Invoke(option, [name]);
    }
}
