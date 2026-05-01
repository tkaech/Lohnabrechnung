using System.Globalization;
using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Domain.Employees;
using Payroll.Domain.Imports;

namespace Payroll.Application.Imports;

public sealed class ImportService
{
    private const decimal MinimumImportHourlyRateChf = 1m;
    private const string PersonFieldPersonnelNumber = "personnel_number";
    private const string PersonFieldFirstName = "first_name";
    private const string PersonFieldLastName = "last_name";
    private const string PersonFieldBirthDate = "birth_date";
    private const string PersonFieldEntryDate = "entry_date";
    private const string PersonFieldExitDate = "exit_date";
    private const string PersonFieldIsActive = "is_active";
    private const string PersonFieldStreet = "street";
    private const string PersonFieldHouseNumber = "house_number";
    private const string PersonFieldAddressLine2 = "address_line2";
    private const string PersonFieldPostalCode = "postal_code";
    private const string PersonFieldCity = "city";
    private const string PersonFieldCountry = "country";
    private const string PersonFieldResidenceCountry = "residence_country";
    private const string PersonFieldNationality = "nationality";
    private const string PersonFieldPermitCode = "permit_code";
    private const string PersonFieldTaxStatus = "tax_status";
    private const string PersonFieldWithholdingTax = "withholding_tax";
    private const string PersonFieldAhvNumber = "ahv_number";
    private const string PersonFieldIban = "iban";
    private const string PersonFieldPhoneNumber = "phone_number";
    private const string PersonFieldEmail = "email";
    private const string PersonFieldWageType = "wage_type";

    private static readonly ImportFieldDefinitionDto[] PersonDataFieldDefinitions =
    [
        new(PersonFieldPersonnelNumber, "Personalnummer", true),
        new(PersonFieldFirstName, "Vorname", true),
        new(PersonFieldLastName, "Nachname", true),
        new(PersonFieldBirthDate, "Geburtsdatum", false),
        new(PersonFieldEntryDate, "Eintrittsdatum", true),
        new(PersonFieldExitDate, "Austrittsdatum", false),
        new(PersonFieldIsActive, "Aktiv / Inaktiv", false),
        new(PersonFieldStreet, "Strasse", true),
        new(PersonFieldHouseNumber, "Hausnummer", false),
        new(PersonFieldAddressLine2, "Adresszusatz", false),
        new(PersonFieldPostalCode, "PLZ", true),
        new(PersonFieldCity, "Ort", true),
        new(PersonFieldCountry, "Land", true),
        new(PersonFieldResidenceCountry, "Wohnsitzland", false),
        new(PersonFieldNationality, "Nationalitaet", false),
        new(PersonFieldPermitCode, "Bewilligung", false),
        new(PersonFieldTaxStatus, "Steuerstatus", false),
        new(PersonFieldWithholdingTax, "Quellensteuerpflicht", false),
        new(PersonFieldAhvNumber, "AHV-Nummer", false),
        new(PersonFieldIban, "IBAN", false),
        new(PersonFieldPhoneNumber, "Telefon", false),
        new(PersonFieldEmail, "E-Mail", false),
        new(PersonFieldWageType, "Lohnart", false)
    ];

    private static readonly ImportFieldDefinitionDto[] TimeDataFieldDefinitions =
    [
        new("personnel_number", "Personalnummer", true),
        new("hours_worked", "Arbeitsstunden", true),
        new("night_hours", "Nachtstunden", false),
        new("sunday_hours", "Sonntagsstunden", false),
        new("holiday_hours", "Feiertagsstunden", false),
        new("vehicle_p1", "Pauschalzone 1", false),
        new("vehicle_p2", "Pauschalzone 2", false),
        new("vehicle_r1", "Regiezone 1", false),
        new("note", "Bemerkung", false)
    ];

    private static readonly CultureInfo[] SupportedCultures =
    [
        CultureInfo.InvariantCulture,
        CultureInfo.GetCultureInfo("de-CH"),
        CultureInfo.GetCultureInfo("de-DE"),
        CultureInfo.GetCultureInfo("en-US")
    ];

    private static readonly string[] SupportedDateFormats =
    [
        "dd.MM.yyyy",
        "d.M.yyyy",
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "MM/dd/yyyy",
        "M/d/yyyy",
        "yyyyMMdd"
    ];

    private static readonly string[] EmptyDatePlaceholderValues =
    [
        "0",
        "00.00.0000",
        "00.00.00",
        "0000-00-00",
        "00000000",
        "-",
        "--"
    ];

    private readonly IImportMappingConfigurationRepository _configurationRepository;
    private readonly ICsvImportFileReader _csvImportFileReader;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmployeeMonthlyRecordRepository _monthlyRecordRepository;
    private readonly IImportExecutionStatusRepository _importExecutionStatusRepository;

    public ImportService(
        IImportMappingConfigurationRepository configurationRepository,
        ICsvImportFileReader csvImportFileReader,
        IEmployeeRepository employeeRepository,
        IEmployeeMonthlyRecordRepository monthlyRecordRepository,
        IImportExecutionStatusRepository importExecutionStatusRepository)
    {
        _configurationRepository = configurationRepository;
        _csvImportFileReader = csvImportFileReader;
        _employeeRepository = employeeRepository;
        _monthlyRecordRepository = monthlyRecordRepository;
        _importExecutionStatusRepository = importExecutionStatusRepository;
    }

    public IReadOnlyCollection<ImportFieldDefinitionDto> GetFieldDefinitions(ImportConfigurationType type)
    {
        return type == ImportConfigurationType.PersonData
            ? PersonDataFieldDefinitions
            : TimeDataFieldDefinitions;
    }

    public Task<IReadOnlyCollection<ImportConfigurationListItemDto>> ListConfigurationsAsync(ImportConfigurationType type, CancellationToken cancellationToken = default)
    {
        return _configurationRepository.ListAsync(type, cancellationToken);
    }

    public Task<ImportConfigurationDto?> GetConfigurationAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        return _configurationRepository.GetByIdAsync(configurationId, cancellationToken);
    }

    public Task<ImportConfigurationDto> SaveConfigurationAsync(SaveImportConfigurationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return _configurationRepository.SaveAsync(command, cancellationToken);
    }

    public Task<CsvImportDocumentDto> ReadCsvDocumentAsync(ReadCsvImportDocumentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return _csvImportFileReader.ReadAsync(command, cancellationToken);
    }

    public ImportValidationResultDto ValidateMappings(
        ImportConfigurationType type,
        IReadOnlyCollection<string> csvHeaders,
        IReadOnlyCollection<ImportFieldMappingDto> mappings)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var headers = csvHeaders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fieldDefinitionsByKey = GetFieldDefinitions(type)
            .ToDictionary(field => field.FieldKey, StringComparer.OrdinalIgnoreCase);
        var mappingByField = mappings
            .Where(item => !string.IsNullOrWhiteSpace(item.CsvColumnName))
            .GroupBy(item => item.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToDictionary(item => item.FieldKey, item => item, StringComparer.OrdinalIgnoreCase);

        foreach (var requiredField in GetFieldDefinitions(type).Where(field => field.IsRequired))
        {
            if (!mappingByField.TryGetValue(requiredField.FieldKey, out var mapping) || string.IsNullOrWhiteSpace(mapping.CsvColumnName))
            {
                errors.Add($"Muss-Feld `{requiredField.Label}` ist noch nicht zugeordnet.");
                continue;
            }

            if (!headers.Contains(mapping.CsvColumnName.Trim()))
            {
                errors.Add($"Zuordnung fuer `{requiredField.Label}` verweist auf unbekannte CSV-Spalte `{mapping.CsvColumnName}`.");
            }
        }

        foreach (var mappedColumn in mappingByField.Values.Select(item => item.CsvColumnName.Trim()))
        {
            if (headers.Contains(mappedColumn))
            {
                continue;
            }

            var mapping = mappingByField.Values.First(item => string.Equals(item.CsvColumnName.Trim(), mappedColumn, StringComparison.OrdinalIgnoreCase));
            if (fieldDefinitionsByKey.TryGetValue(mapping.FieldKey, out var fieldDefinition) && fieldDefinition.IsRequired)
            {
                errors.Add($"CSV-Spalte `{mappedColumn}` ist in der geladenen Datei nicht vorhanden.");
                continue;
            }

            if (fieldDefinitionsByKey.TryGetValue(mapping.FieldKey, out var optionalFieldDefinition))
            {
                warnings.Add($"Optionale Spalte `{mappedColumn}` fuer `{optionalFieldDefinition.Label}` fehlt und wird als 0 bzw. leer importiert.");
            }
        }

        return new ImportValidationResultDto(errors.Count == 0, errors, warnings.Distinct(StringComparer.Ordinal).ToArray());
    }

    public async Task<PersonDataImportResultDto> ImportPersonDataAsync(ImportPersonDataCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await _csvImportFileReader.ReadAsync(
            new ReadCsvImportDocumentCommand(
                command.FilePath,
                command.Delimiter,
                command.FieldsEnclosed,
                command.TextQualifier),
            cancellationToken);

        var validation = ValidateMappings(ImportConfigurationType.PersonData, document.Headers, command.Mappings);
        if (!validation.IsValid)
        {
            return new PersonDataImportResultDto(0, 0, validation.Errors.Count, validation.Errors);
        }

        var mappingByField = command.Mappings
            .Where(item => !string.IsNullOrWhiteSpace(item.CsvColumnName))
            .GroupBy(item => item.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToDictionary(item => item.FieldKey, item => new ImportFieldMappingDto(item.FieldKey, item.CsvColumnName.Trim(), item.AllowEmpty), StringComparer.OrdinalIgnoreCase);

        var createdCount = 0;
        var updatedCount = 0;
        var errorMessages = new List<string>();
        var rowIndex = 1;
        var selectedRowNumbers = command.SelectedRowNumbers?.ToHashSet() ?? [];

        foreach (var row in document.Rows)
        {
            rowIndex++;
            if (selectedRowNumbers.Count > 0 && !selectedRowNumbers.Contains(rowIndex))
            {
                continue;
            }

            if (IsEmptyRow(row))
            {
                continue;
            }

            var rowErrors = new List<string>();
            var personnelNumber = GetRequiredString(row, mappingByField, PersonFieldPersonnelNumber, "Personalnummer", rowErrors);
            var firstName = GetRequiredString(row, mappingByField, PersonFieldFirstName, "Vorname", rowErrors);
            var lastName = GetRequiredString(row, mappingByField, PersonFieldLastName, "Nachname", rowErrors);
            var entryDate = GetRequiredDate(row, mappingByField, PersonFieldEntryDate, "Eintrittsdatum", rowErrors);
            var street = GetRequiredString(row, mappingByField, PersonFieldStreet, "Strasse", rowErrors);
            var postalCode = GetRequiredString(row, mappingByField, PersonFieldPostalCode, "PLZ", rowErrors);
            var city = GetRequiredString(row, mappingByField, PersonFieldCity, "Ort", rowErrors);
            var country = GetRequiredString(row, mappingByField, PersonFieldCountry, "Land", rowErrors);

            var birthDate = GetOptionalDate(row, mappingByField, PersonFieldBirthDate, "Geburtsdatum", rowErrors);
            var exitDate = GetOptionalDate(row, mappingByField, PersonFieldExitDate, "Austrittsdatum", rowErrors);
            var isActive = GetOptionalBoolean(row, mappingByField, PersonFieldIsActive, "Aktiv / Inaktiv", rowErrors);
            var withholdingTax = GetOptionalBoolean(row, mappingByField, PersonFieldWithholdingTax, "Quellensteuerpflicht", rowErrors);
            var wageType = GetOptionalWageType(row, mappingByField, PersonFieldWageType, "Lohnart", rowErrors);
            ValidatePersonDates(birthDate, entryDate, exitDate, rowErrors);

            if (rowErrors.Count > 0)
            {
                errorMessages.Add($"Zeile {rowIndex}: {string.Join(" | ", rowErrors)}");
                continue;
            }

            try
            {
                var existingEmployee = await _employeeRepository.GetByPersonnelNumberAsync(personnelNumber!, cancellationToken);

                var saveCommand = new SaveEmployeeCommand(
                    existingEmployee?.EmployeeId,
                    personnelNumber!,
                    firstName!,
                    lastName!,
                    ResolveValueIfMapped(mappingByField, PersonFieldBirthDate, birthDate, existingEmployee?.BirthDate),
                    entryDate!.Value,
                    ResolveValueIfMapped(mappingByField, PersonFieldExitDate, exitDate, existingEmployee?.ExitDate),
                    ResolveIsActive(mappingByField, isActive, exitDate, existingEmployee),
                    ResolveRequiredString(mappingByField, PersonFieldStreet, street!, existingEmployee?.Street),
                    ResolveOptionalString(mappingByField, PersonFieldHouseNumber, GetOptionalString(row, mappingByField, PersonFieldHouseNumber), existingEmployee?.HouseNumber),
                    ResolveOptionalString(mappingByField, PersonFieldAddressLine2, GetOptionalString(row, mappingByField, PersonFieldAddressLine2), existingEmployee?.AddressLine2),
                    ResolveRequiredString(mappingByField, PersonFieldPostalCode, postalCode!, existingEmployee?.PostalCode),
                    ResolveRequiredString(mappingByField, PersonFieldCity, city!, existingEmployee?.City),
                    ResolveRequiredString(mappingByField, PersonFieldCountry, country!, existingEmployee?.Country),
                    ResolveOptionalString(mappingByField, PersonFieldResidenceCountry, GetOptionalString(row, mappingByField, PersonFieldResidenceCountry), existingEmployee?.ResidenceCountry),
                    ResolveOptionalString(mappingByField, PersonFieldNationality, GetOptionalString(row, mappingByField, PersonFieldNationality), existingEmployee?.Nationality),
                    ResolveOptionalString(mappingByField, PersonFieldPermitCode, GetOptionalString(row, mappingByField, PersonFieldPermitCode), existingEmployee?.PermitCode),
                    ResolveOptionalString(mappingByField, PersonFieldTaxStatus, GetOptionalString(row, mappingByField, PersonFieldTaxStatus), existingEmployee?.TaxStatus),
                    ResolveValueIfMapped(mappingByField, PersonFieldWithholdingTax, withholdingTax, existingEmployee?.IsSubjectToWithholdingTax),
                    ResolveOptionalString(mappingByField, PersonFieldAhvNumber, GetOptionalString(row, mappingByField, PersonFieldAhvNumber), existingEmployee?.AhvNumber),
                    ResolveOptionalString(mappingByField, PersonFieldIban, GetOptionalString(row, mappingByField, PersonFieldIban), existingEmployee?.Iban),
                    ResolveOptionalString(mappingByField, PersonFieldPhoneNumber, GetOptionalString(row, mappingByField, PersonFieldPhoneNumber), existingEmployee?.PhoneNumber),
                    ResolveOptionalString(mappingByField, PersonFieldEmail, GetOptionalString(row, mappingByField, PersonFieldEmail), existingEmployee?.Email),
                    existingEmployee?.DepartmentOptionId,
                    existingEmployee?.EmploymentCategoryOptionId,
                    existingEmployee?.EmploymentLocationOptionId,
                    ResolveWageType(mappingByField, wageType, existingEmployee?.WageType),
                    existingEmployee?.CurrentContractId,
                    existingEmployee?.ContractValidFrom == default || existingEmployee is null
                        ? entryDate.Value
                        : existingEmployee.ContractValidFrom,
                    existingEmployee?.ContractValidTo,
                    ResolveHourlyRate(existingEmployee),
                    existingEmployee?.MonthlyBvgDeductionChf ?? 0m,
                    existingEmployee?.SpecialSupplementRateChf ?? 0m,
                    existingEmployee?.MonthlySalaryAmountChf ?? 0m);

                await _employeeRepository.SaveAsync(saveCommand, cancellationToken);

                if (existingEmployee is null)
                {
                    createdCount++;
                }
                else
                {
                    updatedCount++;
                }
            }
            catch (Exception exception)
            {
                errorMessages.Add($"Zeile {rowIndex}: {BuildImportErrorMessage(exception)}");
            }
        }

        var messages = new List<string>();
        if (createdCount > 0 || updatedCount > 0)
        {
            messages.Add($"{createdCount} neu, {updatedCount} aktualisiert.");
        }

        messages.AddRange(errorMessages);

        return new PersonDataImportResultDto(createdCount, updatedCount, errorMessages.Count, messages);
    }

    public async Task<IReadOnlyCollection<PersonImportPreviewItemDto>> PreviewPersonDataAsync(PreviewPersonDataCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await _csvImportFileReader.ReadAsync(
            new ReadCsvImportDocumentCommand(
                command.FilePath,
                command.Delimiter,
                command.FieldsEnclosed,
                command.TextQualifier),
            cancellationToken);

        var mappingByField = command.Mappings
            .Where(item => !string.IsNullOrWhiteSpace(item.CsvColumnName))
            .GroupBy(item => item.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToDictionary(item => item.FieldKey, item => item, StringComparer.OrdinalIgnoreCase);

        if (!mappingByField.ContainsKey(PersonFieldPersonnelNumber))
        {
            return [];
        }

        var previewItems = new List<PersonImportPreviewItemDto>();
        var rowIndex = 1;
        foreach (var row in document.Rows)
        {
            rowIndex++;
            if (IsEmptyRow(row))
            {
                continue;
            }

            var personnelNumber = GetOptionalString(row, mappingByField, PersonFieldPersonnelNumber);
            if (string.IsNullOrWhiteSpace(personnelNumber))
            {
                continue;
            }

            var firstName = GetOptionalString(row, mappingByField, PersonFieldFirstName);
            var lastName = GetOptionalString(row, mappingByField, PersonFieldLastName);
            var fullName = string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
            var existingEmployee = await _employeeRepository.GetByPersonnelNumberAsync(personnelNumber, cancellationToken);

            previewItems.Add(new PersonImportPreviewItemDto(
                rowIndex,
                personnelNumber,
                fullName,
                existingEmployee is not null));
        }

        return previewItems;
    }

    public async Task<IReadOnlyCollection<TimeImportPreviewItemDto>> PreviewTimeDataAsync(PreviewTimeDataCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        _ = new DateOnly(command.Year, command.Month, 1);

        var document = await _csvImportFileReader.ReadAsync(
            new ReadCsvImportDocumentCommand(
                command.FilePath,
                command.Delimiter,
                command.FieldsEnclosed,
                command.TextQualifier),
            cancellationToken);

        var mappingByField = command.Mappings
            .Where(item => !string.IsNullOrWhiteSpace(item.CsvColumnName))
            .GroupBy(item => item.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToDictionary(item => item.FieldKey, item => item, StringComparer.OrdinalIgnoreCase);

        if (!mappingByField.ContainsKey("personnel_number"))
        {
            return [];
        }

        var previewItems = new List<TimeImportPreviewItemDto>();
        var rowIndex = 1;
        foreach (var row in document.Rows)
        {
            rowIndex++;
            if (IsEmptyRow(row))
            {
                continue;
            }

            var personnelNumber = GetOptionalString(row, mappingByField, "personnel_number");
            if (string.IsNullOrWhiteSpace(personnelNumber))
            {
                continue;
            }

            var employee = await _employeeRepository.GetByPersonnelNumberAsync(personnelNumber, cancellationToken);
            if (employee is null)
            {
                previewItems.Add(new TimeImportPreviewItemDto(
                    rowIndex,
                    personnelNumber,
                    string.Empty,
                    false,
                    false,
                    "Personalnummer nicht gefunden"));
                continue;
            }

            var monthlyDataExists = await _monthlyRecordRepository.HasTimeEntriesAsync(employee.EmployeeId, command.Year, command.Month, cancellationToken);
            previewItems.Add(new TimeImportPreviewItemDto(
                rowIndex,
                personnelNumber,
                $"{employee.FirstName} {employee.LastName}".Trim(),
                true,
                monthlyDataExists,
                monthlyDataExists ? "Monatsdaten vorhanden" : "Import bereit"));
        }

        return previewItems;
    }

    public Task<IReadOnlyCollection<ImportedMonthStatusDto>> ListImportedMonthsAsync(ImportConfigurationType type, CancellationToken cancellationToken = default)
    {
        return _importExecutionStatusRepository.ListAsync(type, cancellationToken);
    }

    public Task<bool> IsMonthImportedAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken = default)
    {
        return _importExecutionStatusRepository.ExistsAsync(type, year, month, cancellationToken);
    }

    public async Task<TimeDataImportResultDto> ImportTimeDataAsync(ImportTimeDataCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        _ = new DateOnly(command.Year, command.Month, 1);

        var document = await _csvImportFileReader.ReadAsync(
            new ReadCsvImportDocumentCommand(command.FilePath, command.Delimiter, command.FieldsEnclosed, command.TextQualifier),
            cancellationToken);

        var validation = ValidateMappings(ImportConfigurationType.TimeData, document.Headers, command.Mappings);
        if (!validation.IsValid)
        {
            return new TimeDataImportResultDto(0, validation.Errors.Count, validation.Errors);
        }

        var selectedRowNumbers = command.SelectedRowNumbers?.ToHashSet() ?? [];
        var importsSubset = selectedRowNumbers.Count > 0;
        var alreadyImported = await _importExecutionStatusRepository.ExistsAsync(ImportConfigurationType.TimeData, command.Year, command.Month, cancellationToken);
        if (alreadyImported && !command.OverwriteExistingMonth && !importsSubset)
        {
            throw new InvalidOperationException("Fuer diesen Monat wurden bereits Stundendaten importiert.");
        }

        if (alreadyImported && command.OverwriteExistingMonth && !importsSubset)
        {
            await _monthlyRecordRepository.DeleteTimeEntriesForMonthAsync(command.Year, command.Month, cancellationToken);
            await _importExecutionStatusRepository.DeleteAsync(ImportConfigurationType.TimeData, command.Year, command.Month, cancellationToken);
        }

        _monthlyRecordRepository.ClearTracking();

        var mappingByField = command.Mappings
            .Where(item => !string.IsNullOrWhiteSpace(item.CsvColumnName))
            .GroupBy(item => item.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToDictionary(item => item.FieldKey, item => new ImportFieldMappingDto(item.FieldKey, item.CsvColumnName.Trim(), item.AllowEmpty), StringComparer.OrdinalIgnoreCase);

        var importedCount = 0;
        var errors = new List<string>();
        var workDate = new DateOnly(command.Year, command.Month, 1);
        var rowIndex = 1;

        foreach (var row in document.Rows)
        {
            rowIndex++;
            if (importsSubset && !selectedRowNumbers.Contains(rowIndex))
            {
                continue;
            }

            if (IsEmptyRow(row))
            {
                continue;
            }

            var rowErrors = new List<string>();
            var personnelNumber = GetRequiredString(row, mappingByField, "personnel_number", "Personalnummer", rowErrors);
            var hoursWorked = GetRequiredDecimal(row, mappingByField, "hours_worked", "Arbeitsstunden", rowErrors);
            var nightHours = GetOptionalDecimal(row, mappingByField, "night_hours", "Nachtstunden", rowErrors) ?? 0m;
            var sundayHours = GetOptionalDecimal(row, mappingByField, "sunday_hours", "Sonntagsstunden", rowErrors) ?? 0m;
            var holidayHours = GetOptionalDecimal(row, mappingByField, "holiday_hours", "Feiertagsstunden", rowErrors) ?? 0m;
            var vehicleP1 = GetOptionalDecimal(row, mappingByField, "vehicle_p1", "Pauschalzone 1", rowErrors) ?? 0m;
            var vehicleP2 = GetOptionalDecimal(row, mappingByField, "vehicle_p2", "Pauschalzone 2", rowErrors) ?? 0m;
            var vehicleR1 = GetOptionalDecimal(row, mappingByField, "vehicle_r1", "Regiezone 1", rowErrors) ?? 0m;
            var note = GetOptionalString(row, mappingByField, "note");

            if (rowErrors.Count > 0)
            {
                errors.Add($"Zeile {rowIndex}: {string.Join(" | ", rowErrors)}");
                continue;
            }

            var employee = await _employeeRepository.GetByPersonnelNumberAsync(personnelNumber!, cancellationToken);
            if (employee is null)
            {
                errors.Add($"Zeile {rowIndex}: Personalnummer `{personnelNumber}` wurde nicht gefunden.");
                continue;
            }

            try
            {
                var monthlyRecord = await _monthlyRecordRepository.GetOrCreateAsync(employee.EmployeeId, command.Year, command.Month, cancellationToken);
                var isNewEntry = monthlyRecord.TimeEntries.Count == 0;
                var timeEntry = monthlyRecord.SaveTimeEntry(
                    null,
                    workDate,
                    hoursWorked!.Value,
                    nightHours,
                    sundayHours,
                    holidayHours,
                    vehicleP1,
                    vehicleP2,
                    vehicleR1,
                    note);

                if (isNewEntry)
                {
                    _monthlyRecordRepository.MarkAsAdded(timeEntry);
                }

                importedCount++;
            }
            catch (Exception exception)
            {
                errors.Add($"Zeile {rowIndex}: {BuildImportErrorMessage(exception)}");
            }
        }

        await _monthlyRecordRepository.SaveChangesAsync(cancellationToken);

        if (importedCount > 0)
        {
            await _importExecutionStatusRepository.MarkImportedAsync(
                ImportConfigurationType.TimeData,
                command.Year,
                command.Month,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }

        var messages = new List<string> { $"{importedCount} importiert." };
        messages.AddRange(errors);
        return new TimeDataImportResultDto(importedCount, errors.Count, messages);
    }

    public async Task DeleteImportedTimeMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        await _monthlyRecordRepository.DeleteTimeEntriesForMonthAsync(year, month, cancellationToken);
        await _importExecutionStatusRepository.DeleteAsync(ImportConfigurationType.TimeData, year, month, cancellationToken);
    }

    private static bool IsEmptyRow(IReadOnlyDictionary<string, string> row)
    {
        return row.Values.All(value => string.IsNullOrWhiteSpace(value));
    }

    private static void ValidatePersonDates(
        DateOnly? birthDate,
        DateOnly? entryDate,
        DateOnly? exitDate,
        ICollection<string> errors)
    {
        if (birthDate.HasValue && entryDate.HasValue && birthDate.Value > entryDate.Value)
        {
            errors.Add("Birth date cannot be after entry date.");
        }

        if (entryDate.HasValue && exitDate.HasValue && exitDate.Value < entryDate.Value)
        {
            errors.Add("Gueltig bis darf nicht vor Gueltig ab liegen.");
        }
    }

    private static string? GetRequiredString(
        IReadOnlyDictionary<string, string> row,
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        string label,
        ICollection<string> errors)
    {
        var value = GetOptionalString(row, mappingByField, fieldKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!IsEmptyAllowed(mappingByField, fieldKey))
            {
                errors.Add($"Muss-Feld `{label}` ist in der CSV-Zeile leer.");
            }
            return null;
        }

        return value;
    }

    private static string? GetOptionalString(
        IReadOnlyDictionary<string, string> row,
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey)
    {
        if (!mappingByField.TryGetValue(fieldKey, out var mapping) || !row.TryGetValue(mapping.CsvColumnName, out var value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static DateOnly? GetRequiredDate(
        IReadOnlyDictionary<string, string> row,
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        string label,
        ICollection<string> errors)
    {
        var value = GetOptionalString(row, mappingByField, fieldKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!IsEmptyAllowed(mappingByField, fieldKey))
            {
                errors.Add($"Muss-Feld `{label}` ist in der CSV-Zeile leer.");
            }
            return null;
        }

        if (!TryParseDateOnly(value, out var parsedDate))
        {
            if (!IsEmptyAllowed(mappingByField, fieldKey))
            {
                errors.Add($"`{label}` hat kein gueltiges Datumsformat.");
            }
            return null;
        }

        return parsedDate;
    }

    private static DateOnly? GetOptionalDate(
        IReadOnlyDictionary<string, string> row,
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        string label,
        ICollection<string> errors)
    {
        var value = GetOptionalString(row, mappingByField, fieldKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (IsEmptyDatePlaceholder(value))
        {
            return null;
        }

        if (!TryParseDateOnly(value, out var parsedDate))
        {
            if (!IsEmptyAllowed(mappingByField, fieldKey))
            {
                errors.Add($"`{label}` hat kein gueltiges Datumsformat.");
            }
            return null;
        }

        return parsedDate;
    }

    private static bool? GetOptionalBoolean(
        IReadOnlyDictionary<string, string> row,
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        string label,
        ICollection<string> errors)
    {
        var value = GetOptionalString(row, mappingByField, fieldKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!TryParseBoolean(value, out var parsed))
        {
            if (!IsEmptyAllowed(mappingByField, fieldKey))
            {
                errors.Add($"`{label}` konnte nicht als Ja/Nein bzw. Aktiv/Inaktiv gelesen werden.");
            }
            return null;
        }

        return parsed;
    }

    private static EmployeeWageType? GetOptionalWageType(
        IReadOnlyDictionary<string, string> row,
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        string label,
        ICollection<string> errors)
    {
        var value = GetOptionalString(row, mappingByField, fieldKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!TryParseWageType(value, out var wageType))
        {
            if (!IsEmptyAllowed(mappingByField, fieldKey))
            {
                errors.Add($"`{label}` konnte nicht als Stundenlohn oder Monatslohn gelesen werden.");
            }
            return null;
        }

        return wageType;
    }

    private static string ResolveRequiredString(
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        string csvValue,
        string? existingValue)
    {
        return mappingByField.ContainsKey(fieldKey)
            ? csvValue
            : existingValue ?? csvValue;
    }

    private static string? ResolveOptionalString(
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        string? csvValue,
        string? existingValue)
    {
        return mappingByField.ContainsKey(fieldKey)
            ? csvValue
            : existingValue;
    }

    private static T? ResolveValueIfMapped<T>(
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        T? csvValue,
        T? existingValue)
        where T : struct
    {
        return mappingByField.ContainsKey(fieldKey)
            ? csvValue
            : existingValue;
    }

    private static bool ResolveIsActive(
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        bool? csvValue,
        DateOnly? exitDate,
        EmployeeDetailsDto? existingEmployee)
    {
        if (mappingByField.ContainsKey(PersonFieldIsActive))
        {
            return csvValue ?? !exitDate.HasValue;
        }

        return existingEmployee?.IsActive ?? !exitDate.HasValue;
    }

    private static EmployeeWageType ResolveWageType(
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        EmployeeWageType? csvValue,
        EmployeeWageType? existingValue)
    {
        if (mappingByField.ContainsKey(PersonFieldWageType))
        {
            return csvValue ?? EmployeeWageType.Hourly;
        }

        return existingValue ?? EmployeeWageType.Hourly;
    }

    private static bool IsEmptyAllowed(
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey)
    {
        return mappingByField.TryGetValue(fieldKey, out var mapping) && mapping.AllowEmpty;
    }

    private static decimal? GetRequiredDecimal(
        IReadOnlyDictionary<string, string> row,
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        string label,
        ICollection<string> errors)
    {
        var value = GetOptionalString(row, mappingByField, fieldKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!IsEmptyAllowed(mappingByField, fieldKey))
            {
                errors.Add($"Muss-Feld `{label}` ist in der CSV-Zeile leer.");
            }

            return null;
        }

        if (!TryParseDecimal(value, out var parsed))
        {
            if (!IsEmptyAllowed(mappingByField, fieldKey))
            {
                errors.Add($"`{label}` hat kein gueltiges Zahlenformat.");
            }

            return null;
        }

        return parsed;
    }

    private static decimal? GetOptionalDecimal(
        IReadOnlyDictionary<string, string> row,
        IReadOnlyDictionary<string, ImportFieldMappingDto> mappingByField,
        string fieldKey,
        string label,
        ICollection<string> errors)
    {
        var value = GetOptionalString(row, mappingByField, fieldKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!TryParseDecimal(value, out var parsed))
        {
            if (!IsEmptyAllowed(mappingByField, fieldKey))
            {
                errors.Add($"`{label}` hat kein gueltiges Zahlenformat.");
            }

            return null;
        }

        return parsed;
    }

    private static decimal ResolveHourlyRate(EmployeeDetailsDto? existingEmployee)
    {
        return existingEmployee?.HourlyRateChf > 0m
            ? existingEmployee.HourlyRateChf
            : MinimumImportHourlyRateChf;
    }

    private static bool IsEmptyDatePlaceholder(string value)
    {
        var normalized = value.Trim();
        return EmptyDatePlaceholderValues.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryParseDateOnly(string value, out DateOnly parsedDate)
    {
        foreach (var format in SupportedDateFormats)
        {
            foreach (var culture in SupportedCultures)
            {
                if (DateOnly.TryParseExact(value, format, culture, DateTimeStyles.None, out parsedDate))
                {
                    return true;
                }
            }
        }

        foreach (var culture in SupportedCultures)
        {
            if (DateTime.TryParse(value, culture, DateTimeStyles.None, out var dateTime))
            {
                parsedDate = DateOnly.FromDateTime(dateTime);
                return true;
            }
        }

        parsedDate = default;
        return false;
    }

    private static bool TryParseBoolean(string value, out bool parsed)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
            case "ja":
            case "yes":
            case "y":
            case "aktiv":
                parsed = true;
                return true;
            case "0":
            case "false":
            case "nein":
            case "no":
            case "n":
            case "inaktiv":
                parsed = false;
                return true;
            default:
                parsed = false;
                return false;
        }
    }

    private static bool TryParseDecimal(string value, out decimal parsed)
    {
        foreach (var culture in SupportedCultures)
        {
            if (decimal.TryParse(value, NumberStyles.Number, culture, out parsed))
            {
                return true;
            }
        }

        var normalized = value.Replace("'", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (decimal.TryParse(normalized.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return true;
        }

        parsed = default;
        return false;
    }

    private static bool TryParseWageType(string value, out EmployeeWageType wageType)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "hourly":
            case "stundenlohn":
            case "stunde":
                wageType = EmployeeWageType.Hourly;
                return true;
            case "monthly":
            case "monatslohn":
            case "monat":
                wageType = EmployeeWageType.Monthly;
                return true;
            default:
                wageType = EmployeeWageType.Hourly;
                return false;
        }
    }

    private static string BuildImportErrorMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException argumentException when !string.IsNullOrWhiteSpace(argumentException.Message)
                => argumentException.Message,
            InvalidOperationException invalidOperationException when !string.IsNullOrWhiteSpace(invalidOperationException.Message)
                => invalidOperationException.Message,
            _ => "Die Zeile konnte nicht gespeichert werden."
        };
    }
}
