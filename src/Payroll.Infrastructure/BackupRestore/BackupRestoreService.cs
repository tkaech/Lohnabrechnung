using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.BackupRestore;
using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Settings;
using Payroll.Domain.Common;
using Payroll.Domain.Employees;
using Payroll.Domain.Expenses;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Settings;
using Payroll.Domain.TimeTracking;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.BackupRestore;

public sealed class BackupRestoreService : IBackupRestoreService
{
    private const string BackupFileExtension = ".payrollbackup.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly Func<PayrollDbContext> _dbContextFactory;
    private readonly EmployeeService _employeeService;
    private readonly MonthlyRecordService _monthlyRecordService;
    private readonly PayrollSettingsService _payrollSettingsService;
    private readonly string _defaultBackupDirectory;

    public BackupRestoreService(
        Func<PayrollDbContext> dbContextFactory,
        EmployeeService employeeService,
        MonthlyRecordService monthlyRecordService,
        PayrollSettingsService payrollSettingsService,
        string defaultBackupDirectory)
    {
        _dbContextFactory = dbContextFactory;
        _employeeService = employeeService;
        _monthlyRecordService = monthlyRecordService;
        _payrollSettingsService = payrollSettingsService;
        _defaultBackupDirectory = defaultBackupDirectory;
    }

    public string GetDefaultBackupDirectory()
    {
        return _defaultBackupDirectory;
    }

    public string CreateDefaultFileName(DateTimeOffset localTimestamp)
    {
        return $"backup_{localTimestamp:yyyy-MM-dd_HH-mm}";
    }

    public async Task<BackupFileInfoDto> CreateBackupAsync(CreateBackupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetDirectoryPath = GuardPath(command.TargetDirectoryPath, nameof(command.TargetDirectoryPath));
        var fileName = GuardFileName(command.FileName);
        Directory.CreateDirectory(targetDirectoryPath);

        var normalizedFileName = fileName.EndsWith(BackupFileExtension, StringComparison.OrdinalIgnoreCase)
            ? fileName
            : fileName + BackupFileExtension;

        var backupFilePath = Path.Combine(targetDirectoryPath, normalizedFileName);
        var createdAtUtc = DateTimeOffset.UtcNow;

        using var dbContext = _dbContextFactory();
        var package = await BuildPackageAsync(dbContext, command.ContentType, createdAtUtc, cancellationToken);

        await using var stream = File.Create(backupFilePath);
        await JsonSerializer.SerializeAsync(stream, package, JsonOptions, cancellationToken);

        return new BackupFileInfoDto(backupFilePath, command.ContentType, createdAtUtc);
    }

    public async Task<RestoreResultDto> RestoreBackupAsync(RestoreBackupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var backupFilePath = GuardPath(command.BackupFilePath, nameof(command.BackupFilePath));
        if (!File.Exists(backupFilePath))
        {
            throw new InvalidOperationException("Backup-Datei wurde nicht gefunden.");
        }

        BackupPackageDto package;
        await using (var stream = File.OpenRead(backupFilePath))
        {
            package = await JsonSerializer.DeserializeAsync<BackupPackageDto>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Backup-Datei konnte nicht gelesen werden.");
        }

        EnsureRestoreTypeIsSupported(package.ContentType, command.ContentType);

        if (IncludesConfiguration(command.ContentType))
        {
            if (package.Configuration is null)
            {
                throw new InvalidOperationException("Backup enthaelt keine Konfiguration.");
            }

            await RestoreConfigurationAsync(package.Configuration, cancellationToken);
        }

        if (IncludesUserData(command.ContentType))
        {
            if (package.UserData is null)
            {
                throw new InvalidOperationException("Backup enthaelt keine Nutzdaten.");
            }

            await RestoreUserDataAsync(package.UserData, cancellationToken);
        }

        return new RestoreResultDto(backupFilePath, command.ContentType, DateTimeOffset.UtcNow);
    }

    private async Task<BackupPackageDto> BuildPackageAsync(
        PayrollDbContext dbContext,
        BackupContentType contentType,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        PayrollSettingsDto? configuration = null;
        UserDataBackupDto? userData = null;

        if (IncludesConfiguration(contentType))
        {
            configuration = await _payrollSettingsService.GetAsync(cancellationToken);
        }

        if (IncludesUserData(contentType))
        {
            userData = await BuildUserDataAsync(dbContext, cancellationToken);
        }

        return new BackupPackageDto(1, createdAtUtc, contentType, configuration, userData);
    }

    private static async Task<UserDataBackupDto> BuildUserDataAsync(PayrollDbContext dbContext, CancellationToken cancellationToken)
    {
        var employees = await dbContext.Employees.AsNoTracking().ToListAsync(cancellationToken);
        var contracts = await dbContext.EmploymentContracts.AsNoTracking().ToListAsync(cancellationToken);
        var departmentOptions = await dbContext.DepartmentOptions.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var categoryOptions = await dbContext.EmploymentCategoryOptions.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var locationOptions = await dbContext.EmploymentLocationOptions.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        var employeeSnapshots = employees
            .Select(employee =>
            {
                var contract = contracts
                    .Where(item => item.EmployeeId == employee.Id)
                    .OrderByDescending(item => item.ValidFrom)
                    .FirstOrDefault();

                return new EmployeeDetailsDto(
                    employee.Id,
                    employee.PersonnelNumber,
                    employee.FirstName,
                    employee.LastName,
                    employee.BirthDate,
                    employee.EntryDate,
                    employee.ExitDate,
                    employee.IsActive,
                    employee.Address.Street,
                    employee.Address.HouseNumber,
                    employee.Address.AddressLine2,
                    employee.Address.PostalCode,
                    employee.Address.City,
                    employee.Address.Country,
                    employee.ResidenceCountry,
                    employee.Nationality,
                    employee.PermitCode,
                    employee.TaxStatus,
                    employee.IsSubjectToWithholdingTax,
                    employee.AhvNumber,
                    employee.Iban,
                    employee.PhoneNumber,
                    employee.Email,
                    employee.DepartmentOptionId,
                    employee.DepartmentOptionId.HasValue && departmentOptions.TryGetValue(employee.DepartmentOptionId.Value, out var departmentName) ? departmentName : null,
                    employee.EmploymentCategoryOptionId,
                    employee.EmploymentCategoryOptionId.HasValue && categoryOptions.TryGetValue(employee.EmploymentCategoryOptionId.Value, out var categoryName) ? categoryName : null,
                    employee.EmploymentLocationOptionId,
                    employee.EmploymentLocationOptionId.HasValue && locationOptions.TryGetValue(employee.EmploymentLocationOptionId.Value, out var locationName) ? locationName : null,
                    contract?.ValidFrom ?? employee.EntryDate,
                    contract?.ValidTo,
                    contract?.HourlyRateChf ?? 0m,
                    contract?.MonthlyBvgDeductionChf ?? 0m,
                    contract?.SpecialSupplementRateChf ?? 0m);
            })
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ToArray();

        var records = await dbContext.EmployeeMonthlyRecords.AsNoTracking().ToListAsync(cancellationToken);
        var timeEntries = await dbContext.TimeEntries.AsNoTracking().ToListAsync(cancellationToken);
        var expenseEntries = await dbContext.ExpenseEntries.AsNoTracking().ToListAsync(cancellationToken);

        var monthlyRecords = records
            .OrderBy(record => record.EmployeeId)
            .ThenBy(record => record.Year)
            .ThenBy(record => record.Month)
            .Select(record => new MonthlyRecordBackupDto(
                record.EmployeeId,
                record.Year,
                record.Month,
                timeEntries
                    .Where(entry => entry.EmployeeMonthlyRecordId == record.Id)
                    .OrderBy(entry => entry.WorkDate)
                    .Select(entry => new TimeEntryBackupDto(
                        entry.WorkDate,
                        entry.HoursWorked,
                        entry.NightHours,
                        entry.SundayHours,
                        entry.HolidayHours,
                        entry.VehiclePauschalzone1Chf,
                        entry.VehiclePauschalzone2Chf,
                        entry.VehicleRegiezone1Chf,
                        entry.Note))
                    .ToArray(),
                expenseEntries
                    .Where(entry => entry.EmployeeMonthlyRecordId == record.Id)
                    .Select(entry => (decimal?)entry.ExpensesTotalChf)
                    .FirstOrDefault()))
            .ToArray();

        return new UserDataBackupDto(employeeSnapshots, monthlyRecords);
    }

    private async Task RestoreConfigurationAsync(PayrollSettingsDto settings, CancellationToken cancellationToken)
    {
        await _payrollSettingsService.SaveAsync(new SavePayrollSettingsCommand(
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
            settings.PrintTemplate,
            settings.NightSupplementRate,
            settings.SundaySupplementRate,
            settings.HolidaySupplementRate,
            settings.AhvIvEoRate,
            settings.AlvRate,
            settings.SicknessAccidentInsuranceRate,
            settings.TrainingAndHolidayRate,
            settings.VacationCompensationRate,
            settings.VehiclePauschalzone1RateChf,
            settings.VehiclePauschalzone2RateChf,
            settings.VehicleRegiezone1RateChf,
            settings.Departments,
            settings.EmploymentCategories,
            settings.EmploymentLocations),
            cancellationToken);
    }

    private async Task RestoreUserDataAsync(UserDataBackupDto userData, CancellationToken cancellationToken)
    {
        using var dbContext = _dbContextFactory();
        await ClearUserDataAsync(dbContext, cancellationToken);

        var settings = await _payrollSettingsService.GetAsync(cancellationToken);

        foreach (var employee in userData.Employees)
        {
            var restoredEmployee = new Employee(
                employee.PersonnelNumber,
                employee.FirstName,
                employee.LastName,
                employee.BirthDate,
                employee.EntryDate,
                employee.ExitDate,
                employee.IsActive,
                new EmployeeAddress(
                    employee.Street,
                    employee.HouseNumber,
                    employee.AddressLine2,
                    employee.PostalCode,
                    employee.City,
                    employee.Country),
                employee.ResidenceCountry,
                employee.Nationality,
                employee.PermitCode,
                employee.TaxStatus,
                employee.IsSubjectToWithholdingTax,
                employee.AhvNumber,
                employee.Iban,
                employee.PhoneNumber,
                employee.Email,
                ResolveOptionId(settings.Departments, employee.DepartmentOptionId, employee.DepartmentName, employee.FirstName + " " + employee.LastName, "Abteilung"),
                ResolveOptionId(settings.EmploymentCategories, employee.EmploymentCategoryOptionId, employee.EmploymentCategoryName, employee.FirstName + " " + employee.LastName, "Anstellungskategorie"),
                ResolveOptionId(settings.EmploymentLocations, employee.EmploymentLocationOptionId, employee.EmploymentLocationName, employee.FirstName + " " + employee.LastName, "Anstellungsort"));

            SetEntityId(restoredEmployee, employee.EmployeeId);
            dbContext.Employees.Add(restoredEmployee);

            var restoredContract = new EmploymentContract(
                employee.EmployeeId,
                employee.ContractValidFrom,
                employee.ContractValidTo,
                employee.HourlyRateChf,
                employee.MonthlyBvgDeductionChf,
                employee.SpecialSupplementRateChf);
            dbContext.EmploymentContracts.Add(restoredContract);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var monthlyRecord in userData.MonthlyRecords)
        {
            var details = await _monthlyRecordService.GetOrCreateAsync(
                new MonthlyRecordQuery(monthlyRecord.EmployeeId, monthlyRecord.Year, monthlyRecord.Month),
                cancellationToken);

            foreach (var timeEntry in monthlyRecord.TimeEntries)
            {
                details = await _monthlyRecordService.SaveTimeEntryAsync(
                    new SaveMonthlyTimeEntryCommand(
                        details.Header.MonthlyRecordId,
                        null,
                        timeEntry.WorkDate,
                        timeEntry.HoursWorked,
                        timeEntry.NightHours,
                        timeEntry.SundayHours,
                        timeEntry.HolidayHours,
                        timeEntry.VehiclePauschalzone1Chf,
                        timeEntry.VehiclePauschalzone2Chf,
                        timeEntry.VehicleRegiezone1Chf,
                        timeEntry.Note),
                    cancellationToken);
            }

            if (monthlyRecord.ExpensesTotalChf.HasValue)
            {
                await _monthlyRecordService.SaveExpenseEntryAsync(
                    new SaveMonthlyExpenseEntryCommand(
                        details.Header.MonthlyRecordId,
                        monthlyRecord.ExpensesTotalChf.Value),
                    cancellationToken);
            }
        }
    }

    private static async Task ClearUserDataAsync(PayrollDbContext dbContext, CancellationToken cancellationToken)
    {
        dbContext.TimeEntries.RemoveRange(await dbContext.TimeEntries.ToListAsync(cancellationToken));
        dbContext.ExpenseEntries.RemoveRange(await dbContext.ExpenseEntries.ToListAsync(cancellationToken));
        dbContext.EmployeeMonthlyRecords.RemoveRange(await dbContext.EmployeeMonthlyRecords.ToListAsync(cancellationToken));
        dbContext.EmploymentContracts.RemoveRange(await dbContext.EmploymentContracts.ToListAsync(cancellationToken));
        dbContext.Employees.RemoveRange(await dbContext.Employees.ToListAsync(cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private static Guid? ResolveOptionId(
        IReadOnlyCollection<SettingOptionDto> options,
        Guid? optionId,
        string? optionName,
        string employeeName,
        string optionLabel)
    {
        if (!optionId.HasValue && string.IsNullOrWhiteSpace(optionName))
        {
            return null;
        }

        var matchingOption = optionId.HasValue
            ? options.FirstOrDefault(item => item.OptionId == optionId.Value)
            : null;

        matchingOption ??= options.FirstOrDefault(item => string.Equals(item.Name, optionName, StringComparison.OrdinalIgnoreCase));

        if (matchingOption is null)
        {
            throw new InvalidOperationException(
                $"Fuer {employeeName} fehlt der Settings-Eintrag '{optionName ?? optionId?.ToString() ?? optionLabel}' im Bereich {optionLabel}. Bitte zuerst Konfiguration oder beides wiederherstellen.");
        }

        return matchingOption.OptionId;
    }

    private static void EnsureRestoreTypeIsSupported(BackupContentType backupType, BackupContentType requestedType)
    {
        if (requestedType == BackupContentType.Both && backupType != BackupContentType.Both)
        {
            throw new InvalidOperationException("Diese Backup-Datei enthaelt nicht gleichzeitig Konfiguration und Nutzdaten.");
        }

        if (requestedType == BackupContentType.Configuration && !IncludesConfiguration(backupType))
        {
            throw new InvalidOperationException("Diese Backup-Datei enthaelt keine Konfiguration.");
        }

        if (requestedType == BackupContentType.UserData && !IncludesUserData(backupType))
        {
            throw new InvalidOperationException("Diese Backup-Datei enthaelt keine Nutzdaten.");
        }
    }

    private static bool IncludesConfiguration(BackupContentType contentType)
    {
        return contentType is BackupContentType.Configuration or BackupContentType.Both;
    }

    private static bool IncludesUserData(BackupContentType contentType)
    {
        return contentType is BackupContentType.UserData or BackupContentType.Both;
    }

    private static string GuardPath(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{parameterName} ist erforderlich.")
            : value.Trim();
    }

    private static void SetEntityId(Entity entity, Guid id)
    {
        typeof(Entity)
            .GetProperty(nameof(Entity.Id))!
            .SetValue(entity, id);
    }

    private static string GuardFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Dateiname ist erforderlich.");
        }

        var trimmedValue = value.Trim();
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            if (trimmedValue.Contains(invalidCharacter))
            {
                throw new InvalidOperationException("Dateiname enthaelt ungueltige Zeichen.");
            }
        }

        return trimmedValue;
    }

    private sealed record BackupPackageDto(
        int Version,
        DateTimeOffset CreatedAtUtc,
        BackupContentType ContentType,
        PayrollSettingsDto? Configuration,
        UserDataBackupDto? UserData);

    private sealed record UserDataBackupDto(
        IReadOnlyCollection<EmployeeDetailsDto> Employees,
        IReadOnlyCollection<MonthlyRecordBackupDto> MonthlyRecords);

    private sealed record MonthlyRecordBackupDto(
        Guid EmployeeId,
        int Year,
        int Month,
        IReadOnlyCollection<TimeEntryBackupDto> TimeEntries,
        decimal? ExpensesTotalChf);

    private sealed record TimeEntryBackupDto(
        DateOnly WorkDate,
        decimal HoursWorked,
        decimal NightHours,
        decimal SundayHours,
        decimal HolidayHours,
        decimal VehiclePauschalzone1Chf,
        decimal VehiclePauschalzone2Chf,
        decimal VehicleRegiezone1Chf,
        string? Note);
}
