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
        await EnsureDefaultCalculationVersionsAsync(settings, cancellationToken);
        return await ToDto(settings, cancellationToken);
    }

    public async Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        await EnsureDefaultCalculationVersionsAsync(settings, cancellationToken);
        var hourlyVersions = await _dbContext.PayrollHourlySettingsVersions.ToListAsync(cancellationToken);
        var current = DetermineCurrentVersion(hourlyVersions);

        return current is null
            ? WorkTimeSupplementSettings.Empty
            : new WorkTimeSupplementSettings(
                current.NightSupplementRate,
                current.SundaySupplementRate,
                current.HolidaySupplementRate);
    }

    public async Task<PayrollSettingsDto> SaveAsync(SavePayrollSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        await EnsureDefaultCalculationVersionsAsync(settings, cancellationToken);

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
            command.AppPagePadding,
            command.AppPanelPadding,
            command.AppSectionSpacing,
            command.AppPanelCornerRadius,
            command.AppTableCellVerticalPadding,
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

        var generalVersions = await _dbContext.PayrollGeneralSettingsVersions.ToListAsync(cancellationToken);
        var hourlyVersions = await _dbContext.PayrollHourlySettingsVersions.ToListAsync(cancellationToken);
        var monthlySalaryVersions = await _dbContext.PayrollMonthlySalarySettingsVersions.ToListAsync(cancellationToken);

        var generalVersion = UpsertGeneralVersion(generalVersions, command);
        var hourlyVersion = UpsertHourlyVersion(hourlyVersions, command);
        var monthlySalaryVersion = UpsertMonthlySalaryVersion(monthlySalaryVersions, command);

        // Legacy projection for the current payroll/snapshot flow until monthly resolution is split by area.
        settings.UpdateWorkTimeSupplementSettings(new WorkTimeSupplementSettings(
            hourlyVersion.NightSupplementRate,
            hourlyVersion.SundaySupplementRate,
            hourlyVersion.HolidaySupplementRate));
        settings.UpdateDeductionAndVehicleRates(
            generalVersion.AhvIvEoRate,
            generalVersion.AlvRate,
            generalVersion.SicknessAccidentInsuranceRate,
            generalVersion.TrainingAndHolidayRate,
            hourlyVersion.VacationCompensationRate,
            hourlyVersion.VacationCompensationRateAge50Plus,
            hourlyVersion.VehiclePauschalzone1RateChf,
            hourlyVersion.VehiclePauschalzone2RateChf,
            hourlyVersion.VehicleRegiezone1RateChf);

        await SyncOptionsAsync(_dbContext.DepartmentOptions, command.Departments, nameof(global::Payroll.Domain.Employees.Employee.DepartmentOptionId), "Abteilung", cancellationToken);
        await SyncOptionsAsync(_dbContext.EmploymentCategoryOptions, command.EmploymentCategories, nameof(global::Payroll.Domain.Employees.Employee.EmploymentCategoryOptionId), "Anstellungskategorie", cancellationToken);
        await SyncOptionsAsync(_dbContext.EmploymentLocationOptions, command.EmploymentLocations, nameof(global::Payroll.Domain.Employees.Employee.EmploymentLocationOptionId), "Anstellungsort", cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await ToDto(settings, cancellationToken);
    }

    private PayrollGeneralSettingsVersion UpsertGeneralVersion(
        List<PayrollGeneralSettingsVersion> versions,
        SavePayrollSettingsCommand command)
    {
        var version = ResolveVersion(
            versions,
            command.EditingGeneralSettingsVersionId,
            () =>
            {
                var created = new PayrollGeneralSettingsVersion();
                _dbContext.PayrollGeneralSettingsVersions.Add(created);
                versions.Add(created);
                return created;
            });

        version.UpdateValidity(
            command.GeneralSettingsValidFrom == default ? ResolveDefaultValidFrom(versions) : command.GeneralSettingsValidFrom,
            command.GeneralSettingsValidTo);
        version.UpdateRates(
            command.AhvIvEoRate,
            command.AlvRate,
            command.SicknessAccidentInsuranceRate,
            command.TrainingAndHolidayRate);

        ApplyVersionRules("Allgemein", versions, version, isNewVersion: !command.EditingGeneralSettingsVersionId.HasValue);
        return version;
    }

    private PayrollHourlySettingsVersion UpsertHourlyVersion(
        List<PayrollHourlySettingsVersion> versions,
        SavePayrollSettingsCommand command)
    {
        var version = ResolveVersion(
            versions,
            command.EditingHourlySettingsVersionId,
            () =>
            {
                var created = new PayrollHourlySettingsVersion();
                _dbContext.PayrollHourlySettingsVersions.Add(created);
                versions.Add(created);
                return created;
            });

        version.UpdateValidity(
            command.HourlySettingsValidFrom == default ? ResolveDefaultValidFrom(versions) : command.HourlySettingsValidFrom,
            command.HourlySettingsValidTo);
        version.UpdateRates(
            command.NightSupplementRate,
            command.SundaySupplementRate,
            command.HolidaySupplementRate,
            command.VacationCompensationRate,
            command.VacationCompensationRateAge50Plus,
            command.VehiclePauschalzone1RateChf,
            command.VehiclePauschalzone2RateChf,
            command.VehicleRegiezone1RateChf);

        ApplyVersionRules("Stundenlohn", versions, version, isNewVersion: !command.EditingHourlySettingsVersionId.HasValue);
        return version;
    }

    private PayrollMonthlySalarySettingsVersion UpsertMonthlySalaryVersion(
        List<PayrollMonthlySalarySettingsVersion> versions,
        SavePayrollSettingsCommand command)
    {
        var version = ResolveVersion(
            versions,
            command.EditingMonthlySalarySettingsVersionId,
            () =>
            {
                var created = new PayrollMonthlySalarySettingsVersion();
                _dbContext.PayrollMonthlySalarySettingsVersions.Add(created);
                versions.Add(created);
                return created;
            });

        version.UpdateValidity(
            command.MonthlySalarySettingsValidFrom == default ? ResolveDefaultValidFrom(versions) : command.MonthlySalarySettingsValidFrom,
            command.MonthlySalarySettingsValidTo);
        version.MarkPrepared();

        ApplyVersionRules("Monatslohn", versions, version, isNewVersion: !command.EditingMonthlySalarySettingsVersionId.HasValue);
        return version;
    }

    private async Task<PayrollSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.Set<PayrollSettings>().SingleOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            await EnsureDefaultOptionsAsync(cancellationToken);
            if (_dbContext.ChangeTracker.HasChanges())
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return settings;
        }

        settings = new PayrollSettings();
        _dbContext.Set<PayrollSettings>().Add(settings);
        await EnsureDefaultOptionsAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private async Task EnsureDefaultCalculationVersionsAsync(PayrollSettings settings, CancellationToken cancellationToken)
    {
        var defaultValidFrom = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

        if (!await _dbContext.PayrollGeneralSettingsVersions.AnyAsync(cancellationToken))
        {
            var version = new PayrollGeneralSettingsVersion();
            version.UpdateValidity(defaultValidFrom, null);
            version.UpdateRates(
                settings.AhvIvEoRate,
                settings.AlvRate,
                settings.SicknessAccidentInsuranceRate,
                settings.TrainingAndHolidayRate);
            _dbContext.PayrollGeneralSettingsVersions.Add(version);
        }

        if (!await _dbContext.PayrollHourlySettingsVersions.AnyAsync(cancellationToken))
        {
            var version = new PayrollHourlySettingsVersion();
            version.UpdateValidity(defaultValidFrom, null);
            version.UpdateRates(
                settings.WorkTimeSupplementSettings.NightSupplementRate,
                settings.WorkTimeSupplementSettings.SundaySupplementRate,
                settings.WorkTimeSupplementSettings.HolidaySupplementRate,
                settings.VacationCompensationRate,
                settings.VacationCompensationRateAge50Plus,
                settings.VehiclePauschalzone1RateChf,
                settings.VehiclePauschalzone2RateChf,
                settings.VehicleRegiezone1RateChf);
            _dbContext.PayrollHourlySettingsVersions.Add(version);
        }

        if (!await _dbContext.PayrollMonthlySalarySettingsVersions.AnyAsync(cancellationToken))
        {
            var version = new PayrollMonthlySalarySettingsVersion();
            version.UpdateValidity(defaultValidFrom, null);
            version.MarkPrepared();
            _dbContext.PayrollMonthlySalarySettingsVersions.Add(version);
        }

        await EnsureDefaultOptionsAsync(cancellationToken);

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
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

    private async Task<PayrollSettingsDto> ToDto(PayrollSettings settings, CancellationToken cancellationToken)
    {
        var printTemplate = string.IsNullOrWhiteSpace(settings.PrintTemplate)
            ? PayrollStatementTemplateProvider.LoadDefaultTemplate()
            : settings.PrintTemplate;

        var generalVersions = await _dbContext.PayrollGeneralSettingsVersions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var hourlyVersions = await _dbContext.PayrollHourlySettingsVersions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var monthlySalaryVersions = await _dbContext.PayrollMonthlySalarySettingsVersions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var currentGeneral = DetermineCurrentVersion(generalVersions);
        var currentHourly = DetermineCurrentVersion(hourlyVersions);
        var currentMonthlySalary = DetermineCurrentVersion(monthlySalaryVersions);

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
            currentHourly?.NightSupplementRate,
            currentHourly?.SundaySupplementRate,
            currentHourly?.HolidaySupplementRate,
            currentGeneral?.AhvIvEoRate ?? settings.AhvIvEoRate,
            currentGeneral?.AlvRate ?? settings.AlvRate,
            currentGeneral?.SicknessAccidentInsuranceRate ?? settings.SicknessAccidentInsuranceRate,
            currentGeneral?.TrainingAndHolidayRate ?? settings.TrainingAndHolidayRate,
            currentHourly?.VacationCompensationRate ?? settings.VacationCompensationRate,
            currentHourly?.VacationCompensationRateAge50Plus ?? settings.VacationCompensationRateAge50Plus,
            currentHourly?.VehiclePauschalzone1RateChf ?? settings.VehiclePauschalzone1RateChf,
            currentHourly?.VehiclePauschalzone2RateChf ?? settings.VehiclePauschalzone2RateChf,
            currentHourly?.VehicleRegiezone1RateChf ?? settings.VehicleRegiezone1RateChf,
            LoadPayrollPreviewHelpOptions(settings),
            await LoadOptionsAsync(_dbContext.DepartmentOptions, cancellationToken),
            await LoadOptionsAsync(_dbContext.EmploymentCategoryOptions, cancellationToken),
            await LoadOptionsAsync(_dbContext.EmploymentLocationOptions, cancellationToken),
            currentGeneral?.Id,
            currentGeneral?.ValidFrom ?? default,
            currentGeneral?.ValidTo,
            BuildGeneralHistory(generalVersions, currentGeneral?.Id),
            currentHourly?.Id,
            currentHourly?.ValidFrom ?? default,
            currentHourly?.ValidTo,
            BuildHourlyHistory(hourlyVersions, currentHourly?.Id),
            currentMonthlySalary?.Id,
            currentMonthlySalary?.ValidFrom ?? default,
            currentMonthlySalary?.ValidTo,
            BuildMonthlySalaryHistory(monthlySalaryVersions, currentMonthlySalary?.Id),
            settings.AppPagePadding,
            settings.AppPanelPadding,
            settings.AppSectionSpacing,
            settings.AppPanelCornerRadius,
            settings.AppTableCellVerticalPadding);
    }

    private static IReadOnlyCollection<PayrollGeneralSettingsVersionDto> BuildGeneralHistory(
        IReadOnlyCollection<PayrollGeneralSettingsVersion> versions,
        Guid? currentVersionId)
    {
        return versions
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .Select(item => new PayrollGeneralSettingsVersionDto(
                item.Id,
                item.ValidFrom,
                item.ValidTo,
                item.AhvIvEoRate,
                item.AlvRate,
                item.SicknessAccidentInsuranceRate,
                item.TrainingAndHolidayRate,
                item.Id == currentVersionId))
            .ToArray();
    }

    private static IReadOnlyCollection<PayrollHourlySettingsVersionDto> BuildHourlyHistory(
        IReadOnlyCollection<PayrollHourlySettingsVersion> versions,
        Guid? currentVersionId)
    {
        return versions
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .Select(item => new PayrollHourlySettingsVersionDto(
                item.Id,
                item.ValidFrom,
                item.ValidTo,
                item.NightSupplementRate,
                item.SundaySupplementRate,
                item.HolidaySupplementRate,
                item.VacationCompensationRate,
                item.VacationCompensationRateAge50Plus,
                item.VehiclePauschalzone1RateChf,
                item.VehiclePauschalzone2RateChf,
                item.VehicleRegiezone1RateChf,
                item.Id == currentVersionId))
            .ToArray();
    }

    private static IReadOnlyCollection<PayrollMonthlySalarySettingsVersionDto> BuildMonthlySalaryHistory(
        IReadOnlyCollection<PayrollMonthlySalarySettingsVersion> versions,
        Guid? currentVersionId)
    {
        return versions
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .Select(item => new PayrollMonthlySalarySettingsVersionDto(
                item.Id,
                item.ValidFrom,
                item.ValidTo,
                item.Id == currentVersionId))
            .ToArray();
    }

    private static T ResolveVersion<T>(
        IEnumerable<T> versions,
        Guid? editingId,
        Func<T> create)
        where T : PayrollCalculationSettingsVersionBase
    {
        if (!editingId.HasValue)
        {
            return create();
        }

        return versions.SingleOrDefault(item => item.Id == editingId.Value)
               ?? throw new InvalidOperationException("Der zu bearbeitende Satzstand wurde nicht gefunden.");
    }

    private static DateOnly ResolveDefaultValidFrom<T>(IReadOnlyCollection<T> versions)
        where T : PayrollCalculationSettingsVersionBase
    {
        var current = DetermineCurrentVersion(versions);
        return current?.ValidFrom ?? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    private static T? DetermineCurrentVersion<T>(IReadOnlyCollection<T> versions)
        where T : PayrollCalculationSettingsVersionBase
    {
        var referenceMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

        return versions
            .Where(item => item.ValidFrom <= referenceMonth && (!item.ValidTo.HasValue || item.ValidTo.Value >= referenceMonth))
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault()
            ?? versions
                .OrderByDescending(item => item.ValidFrom)
                .ThenByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault();
    }

    private static void ApplyVersionRules<T>(
        string areaLabel,
        IReadOnlyCollection<T> versions,
        T updatedVersion,
        bool isNewVersion)
        where T : PayrollCalculationSettingsVersionBase
    {
        var orderedVersions = versions
            .OrderBy(item => item.ValidFrom)
            .ThenBy(item => item.CreatedAtUtc)
            .ToArray();

        var currentIndex = Array.FindIndex(orderedVersions, item => item.Id == updatedVersion.Id);
        if (currentIndex < 0)
        {
            throw new InvalidOperationException($"{areaLabel}-Stand konnte nicht zugeordnet werden.");
        }

        if (updatedVersion.ValidTo.HasValue && updatedVersion.ValidTo.Value < updatedVersion.ValidFrom)
        {
            throw new InvalidOperationException($"Der {areaLabel}-Stand hat einen ungueltigen Gueltigkeitsbereich.");
        }

        var previous = currentIndex > 0 ? orderedVersions[currentIndex - 1] : null;
        var next = currentIndex < orderedVersions.Length - 1 ? orderedVersions[currentIndex + 1] : null;

        if (isNewVersion)
        {
            if (previous is not null)
            {
                previous.UpdateValidity(previous.ValidFrom, updatedVersion.ValidFrom.AddDays(-1));
            }

            if (!updatedVersion.ValidTo.HasValue && next is not null)
            {
                updatedVersion.UpdateValidity(updatedVersion.ValidFrom, next.ValidFrom.AddDays(-1));
            }
        }

        if (previous is not null && previous.ValidTo.HasValue && previous.ValidTo.Value >= updatedVersion.ValidFrom)
        {
            throw new InvalidOperationException($"{areaLabel}-Satzstaende duerfen sich nicht ueberschneiden.");
        }

        if (next is not null)
        {
            var effectiveValidTo = updatedVersion.ValidTo ?? DateOnly.MaxValue;
            if (effectiveValidTo >= next.ValidFrom)
            {
                throw new InvalidOperationException($"{areaLabel}-Satzstaende duerfen sich nicht ueberschneiden.");
            }
        }
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
