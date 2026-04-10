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
        return await ToDto(settings, cancellationToken);
    }

    public async Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken)
    {
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
        await SyncOptionsAsync(_dbContext.DepartmentOptions, command.Departments, nameof(global::Payroll.Domain.Employees.Employee.DepartmentOptionId), "Abteilung", cancellationToken);
        await SyncOptionsAsync(_dbContext.EmploymentCategoryOptions, command.EmploymentCategories, nameof(global::Payroll.Domain.Employees.Employee.EmploymentCategoryOptionId), "Anstellungskategorie", cancellationToken);
        await SyncOptionsAsync(_dbContext.EmploymentLocationOptions, command.EmploymentLocations, nameof(global::Payroll.Domain.Employees.Employee.EmploymentLocationOptionId), "Anstellungsort", cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await ToDto(settings, cancellationToken);
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
            settings.WorkTimeSupplementSettings.NightSupplementRate,
            settings.WorkTimeSupplementSettings.SundaySupplementRate,
            settings.WorkTimeSupplementSettings.HolidaySupplementRate,
            settings.AhvIvEoRate,
            settings.AlvRate,
            settings.SicknessAccidentInsuranceRate,
            settings.TrainingAndHolidayRate,
            settings.VacationCompensationRate,
            settings.VacationCompensationRateAge50Plus,
            settings.VehiclePauschalzone1RateChf,
            settings.VehiclePauschalzone2RateChf,
            settings.VehicleRegiezone1RateChf,
            LoadPayrollPreviewHelpOptions(settings),
            await LoadOptionsAsync(_dbContext.DepartmentOptions, cancellationToken),
            await LoadOptionsAsync(_dbContext.EmploymentCategoryOptions, cancellationToken),
            await LoadOptionsAsync(_dbContext.EmploymentLocationOptions, cancellationToken));
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
