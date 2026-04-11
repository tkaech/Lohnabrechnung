using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Employees;
using Payroll.Application.Imports;
using Payroll.Application.MonthlyRecords;
using Payroll.Domain.Employees;
using Payroll.Domain.Imports;
using Payroll.Domain.MonthlyRecords;
using Payroll.Infrastructure.Imports;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Application.Tests;

public sealed class ImportServiceTests
{
    [Fact]
    public async Task SaveAndLoadConfiguration_PersistsPersonDataMapping()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new ImportMappingConfigurationRepository(dbContext);

        var saved = await repository.SaveAsync(new SaveImportConfigurationCommand(
            null,
            ImportConfigurationType.PersonData,
            "Standard Personendaten",
            ";",
            true,
            "\"",
            [
                new ImportFieldMappingDto("personnel_number", "PNr", false),
                new ImportFieldMappingDto("first_name", "Vorname", true)
            ]),
            CancellationToken.None);

        var loaded = await repository.GetByIdAsync(saved.ConfigurationId, CancellationToken.None);
        var listed = await repository.ListAsync(ImportConfigurationType.PersonData, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("Standard Personendaten", loaded!.Name);
        Assert.Equal(";", loaded.Delimiter);
        Assert.Equal("\"", loaded.TextQualifier);
        Assert.Contains(loaded.Mappings, item => item.FieldKey == "personnel_number" && item.CsvColumnName == "PNr" && !item.AllowEmpty);
        Assert.Contains(loaded.Mappings, item => item.FieldKey == "first_name" && item.CsvColumnName == "Vorname" && item.AllowEmpty);
        Assert.Contains(listed, item => item.ConfigurationId == saved.ConfigurationId && item.Name == "Standard Personendaten");
    }

    [Fact]
    public void ValidateMappings_RejectsMissingRequiredFields()
    {
        var service = CreateImportService(new InMemoryEmployeeRepository());

        var result = service.ValidateMappings(
            ImportConfigurationType.PersonData,
            ["Vorname", "Nachname"],
            [
                new ImportFieldMappingDto("first_name", "Vorname", false),
                new ImportFieldMappingDto("last_name", "Nachname", false)
            ]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Personalnummer", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReadCsvDocument_ReturnsHeadersForDropdownOptions()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname\n1000;Anna;Aktiv\n");

        try
        {
            var reader = new CsvImportFileReader();
            var result = await reader.ReadAsync(
                new ReadCsvImportDocumentCommand(filePath, ";", true, "\""),
                CancellationToken.None);

            Assert.Equal(["PNr", "Vorname", "Nachname"], result.Headers);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadCsvDocument_ToleratesMixedQuotedAndUnquotedFields()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname;Ort\n'1000';Anna;\"Muster\";'St. Gallen'\n");

        try
        {
            var reader = new CsvImportFileReader();
            var result = await reader.ReadAsync(
                new ReadCsvImportDocumentCommand(filePath, ";", true, "\""),
                CancellationToken.None);

            var row = Assert.Single(result.Rows);
            Assert.Equal("1000", row["PNr"]);
            Assert.Equal("Anna", row["Vorname"]);
            Assert.Equal("Muster", row["Nachname"]);
            Assert.Equal("St. Gallen", row["Ort"]);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportPersonData_UsesPersonnelNumberAsUniqueKey_AndUpdatesExistingEmployee()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname;Eintritt;Strasse;PLZ;Ort;Land;Telefon\n1000;Anna;Neu;01.01.2025;Hauptstrasse;6000;Luzern;Schweiz;+41 79 999 00 00\n");

        try
        {
            var employeeRepository = new InMemoryEmployeeRepository();
            await employeeRepository.SaveAsync(CreateEmployeeCommand(
                employeeId: null,
                personnelNumber: "1000",
                firstName: "Anna",
                lastName: "Alt",
                city: "Bern",
                phoneNumber: "+41 79 111 00 00"),
                CancellationToken.None);

            var service = CreateImportService(employeeRepository);

            var result = await service.ImportPersonDataAsync(new ImportPersonDataCommand(
                filePath,
                ";",
                true,
                "\"",
                BuildMappings(("personnel_number", "PNr", false), ("first_name", "Vorname", false), ("last_name", "Nachname", false), ("entry_date", "Eintritt", false), ("street", "Strasse", false), ("postal_code", "PLZ", false), ("city", "Ort", false), ("country", "Land", false), ("phone_number", "Telefon", false))),
                CancellationToken.None);

            var employees = await employeeRepository.ListAsync(new EmployeeListQuery(null, null), CancellationToken.None);
            var updated = await employeeRepository.GetByPersonnelNumberAsync("1000", CancellationToken.None);

            Assert.Single(employees);
            Assert.Equal(0, result.CreatedCount);
            Assert.Equal(1, result.UpdatedCount);
            Assert.NotNull(updated);
            Assert.Equal("Neu", updated!.LastName);
            Assert.Equal("+41 79 999 00 00", updated.PhoneNumber);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task PreviewPersonData_MarksExistingAndNewEmployees()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname;Eintritt;Strasse;PLZ;Ort;Land\n1000;Anna;Alt;01.01.2025;Hauptstrasse;6000;Luzern;Schweiz\n2000;Mia;Neu;01.01.2025;Dorfweg;5000;Aarau;Schweiz\n");

        try
        {
            var employeeRepository = new InMemoryEmployeeRepository();
            await employeeRepository.SaveAsync(CreateEmployeeCommand(
                null,
                "1000",
                "Anna",
                "Bestehend",
                "Bern"),
                CancellationToken.None);

            var service = CreateImportService(employeeRepository);

            var preview = await service.PreviewPersonDataAsync(new PreviewPersonDataCommand(
                filePath,
                ";",
                true,
                "\"",
                BuildMappings(
                    ("personnel_number", "PNr", false),
                    ("first_name", "Vorname", false),
                    ("last_name", "Nachname", false),
                    ("entry_date", "Eintritt", false),
                    ("street", "Strasse", false),
                    ("postal_code", "PLZ", false),
                    ("city", "Ort", false),
                    ("country", "Land", false))),
                CancellationToken.None);

            Assert.Collection(
                preview.OrderBy(item => item.PersonnelNumber),
                first =>
                {
                    Assert.Equal("1000", first.PersonnelNumber);
                    Assert.True(first.AlreadyExists);
                },
                second =>
                {
                    Assert.Equal("2000", second.PersonnelNumber);
                    Assert.False(second.AlreadyExists);
                });
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportPersonData_CreatesNewEmployee_WhenPersonnelNumberDoesNotExist()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname;Eintritt;Strasse;PLZ;Ort;Land;Lohnart\n2000;Mia;Muster;2025-02-01;Dorfweg;5000;Aarau;Schweiz;Monatslohn\n");

        try
        {
            var employeeRepository = new InMemoryEmployeeRepository();
            var service = CreateImportService(employeeRepository);

            var result = await service.ImportPersonDataAsync(new ImportPersonDataCommand(
                filePath,
                ";",
                true,
                "\"",
                BuildMappings(("personnel_number", "PNr", false), ("first_name", "Vorname", false), ("last_name", "Nachname", false), ("entry_date", "Eintritt", false), ("street", "Strasse", false), ("postal_code", "PLZ", false), ("city", "Ort", false), ("country", "Land", false), ("wage_type", "Lohnart", false))),
                CancellationToken.None);

            var created = await employeeRepository.GetByPersonnelNumberAsync("2000", CancellationToken.None);

            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(0, result.UpdatedCount);
            Assert.NotNull(created);
            Assert.Equal(EmployeeWageType.Monthly, created!.WageType);
            Assert.True(created.HourlyRateChf > 0m);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportPersonData_ImportsOnlySelectedRows()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname;Eintritt;Strasse;PLZ;Ort;Land\n2000;Mia;Muster;2025-02-01;Dorfweg;5000;Aarau;Schweiz\n2001;Noah;NichtImportieren;2025-02-01;Dorfweg;5000;Aarau;Schweiz\n");

        try
        {
            var employeeRepository = new InMemoryEmployeeRepository();
            var service = CreateImportService(employeeRepository);

            var result = await service.ImportPersonDataAsync(new ImportPersonDataCommand(
                filePath,
                ";",
                true,
                "\"",
                BuildMappings(
                    ("personnel_number", "PNr", false),
                    ("first_name", "Vorname", false),
                    ("last_name", "Nachname", false),
                    ("entry_date", "Eintritt", false),
                    ("street", "Strasse", false),
                    ("postal_code", "PLZ", false),
                    ("city", "Ort", false),
                    ("country", "Land", false)),
                [2]),
                CancellationToken.None);

            var imported = await employeeRepository.GetByPersonnelNumberAsync("2000", CancellationToken.None);
            var skipped = await employeeRepository.GetByPersonnelNumberAsync("2001", CancellationToken.None);

            Assert.Equal(1, result.CreatedCount);
            Assert.NotNull(imported);
            Assert.Null(skipped);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportPersonData_TreatsCommonExitDatePlaceholdersAsEmpty()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname;Eintritt;Austritt;Strasse;PLZ;Ort;Land\n2100;Mia;Muster;2025-02-01;00.00.0000;Dorfweg;5000;Aarau;Schweiz\n");

        try
        {
            var employeeRepository = new InMemoryEmployeeRepository();
            var service = CreateImportService(employeeRepository);

            var result = await service.ImportPersonDataAsync(new ImportPersonDataCommand(
                filePath,
                ";",
                true,
                "\"",
                BuildMappings(
                    ("personnel_number", "PNr", false),
                    ("first_name", "Vorname", false),
                    ("last_name", "Nachname", false),
                    ("entry_date", "Eintritt", false),
                    ("exit_date", "Austritt", false),
                    ("street", "Strasse", false),
                    ("postal_code", "PLZ", false),
                    ("city", "Ort", false),
                    ("country", "Land", false))),
                CancellationToken.None);

            var created = await employeeRepository.GetByPersonnelNumberAsync("2100", CancellationToken.None);

            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(0, result.ErrorCount);
            Assert.NotNull(created);
            Assert.Null(created!.ExitDate);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportPersonData_ReturnsRowError_WhenEmployeeDataViolatesDomainRules()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname;Geburt;Eintritt;Strasse;PLZ;Ort;Land\n3000;Lia;Beispiel;01.01.2026;01.01.2025;Dorfweg;5000;Aarau;Schweiz\n");

        try
        {
            var service = CreateImportService(new InMemoryEmployeeRepository());

            var result = await service.ImportPersonDataAsync(new ImportPersonDataCommand(
                filePath,
                ";",
                true,
                "\"",
                BuildMappings(
                    ("personnel_number", "PNr", false),
                    ("first_name", "Vorname", false),
                    ("last_name", "Nachname", false),
                    ("birth_date", "Geburt", false),
                    ("entry_date", "Eintritt", false),
                    ("street", "Strasse", false),
                    ("postal_code", "PLZ", false),
                    ("city", "Ort", false),
                    ("country", "Land", false))),
                CancellationToken.None);

            Assert.Equal(0, result.CreatedCount);
            Assert.Equal(0, result.UpdatedCount);
            Assert.Equal(1, result.ErrorCount);
            Assert.Contains(result.Messages, message => message.Contains("Birth date cannot be after entry date.", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportPersonData_AllowsEmptyOptionalField_WhenConfigured()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname;Eintritt;Austritt;Strasse;PLZ;Ort;Land\n3100;Mia;Leer;2025-02-01;;Dorfweg;5000;Aarau;Schweiz\n");

        try
        {
            var employeeRepository = new InMemoryEmployeeRepository();
            var service = CreateImportService(employeeRepository);

            var result = await service.ImportPersonDataAsync(new ImportPersonDataCommand(
                filePath,
                ";",
                true,
                "\"",
                BuildMappings(
                    ("personnel_number", "PNr", false),
                    ("first_name", "Vorname", false),
                    ("last_name", "Nachname", false),
                    ("entry_date", "Eintritt", false),
                    ("exit_date", "Austritt", true),
                    ("street", "Strasse", false),
                    ("postal_code", "PLZ", false),
                    ("city", "Ort", false),
                    ("country", "Land", false))),
                CancellationToken.None);

            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(0, result.ErrorCount);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportPersonData_IgnoresInvalidOptionalValue_WhenAllowEmptyIsEnabled()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"person-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "PNr;Vorname;Nachname;Eintritt;Austritt;Strasse;PLZ;Ort;Land\n3200;Mia;Ungueltig;2025-02-01;n/a;Dorfweg;5000;Aarau;Schweiz\n");

        try
        {
            var employeeRepository = new InMemoryEmployeeRepository();
            var service = CreateImportService(employeeRepository);

            var result = await service.ImportPersonDataAsync(new ImportPersonDataCommand(
                filePath,
                ";",
                true,
                "\"",
                BuildMappings(
                    ("personnel_number", "PNr", false),
                    ("first_name", "Vorname", false),
                    ("last_name", "Nachname", false),
                    ("entry_date", "Eintritt", false),
                    ("exit_date", "Austritt", true),
                    ("street", "Strasse", false),
                    ("postal_code", "PLZ", false),
                    ("city", "Ort", false),
                    ("country", "Land", false))),
                CancellationToken.None);

            var created = await employeeRepository.GetByPersonnelNumberAsync("3200", CancellationToken.None);
            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(0, result.ErrorCount);
            Assert.NotNull(created);
            Assert.Null(created!.ExitDate);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportTimeData_RequiresSelectedMonthInCommand()
    {
        var employeeRepository = new InMemoryEmployeeRepository();
        await employeeRepository.SaveAsync(CreateEmployeeCommand(null, "1000", "Anna", "Aktiv", "Bern"), CancellationToken.None);
        var service = CreateImportService(employeeRepository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.ImportTimeDataAsync(new ImportTimeDataCommand(
                "irrelevant.csv",
                ";",
                true,
                "\"",
                2026,
                13,
                false,
                BuildMappings(
                    ("personnel_number", "Personalnummer", false),
                    ("hours_worked", "Stunden", false))),
                CancellationToken.None));
    }

    [Fact]
    public async Task ImportTimeData_UsesSelectedMonthAsImportContext()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"time-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "Personalnummer;Stunden;Nacht\n1000;12.5;2\n");

        try
        {
            var employeeRepository = new InMemoryEmployeeRepository();
            await employeeRepository.SaveAsync(CreateEmployeeCommand(null, "1000", "Anna", "Aktiv", "Bern"), CancellationToken.None);
            var monthlyRecordRepository = new InMemoryEmployeeMonthlyRecordRepository();
            var statusRepository = new InMemoryImportExecutionStatusRepository();
            var service = CreateImportService(employeeRepository, monthlyRecordRepository, statusRepository);

            var result = await service.ImportTimeDataAsync(new ImportTimeDataCommand(
                filePath,
                ";",
                true,
                "\"",
                2026,
                4,
                false,
                BuildMappings(
                    ("personnel_number", "Personalnummer", false),
                    ("hours_worked", "Stunden", false),
                    ("night_hours", "Nacht", true))),
                CancellationToken.None);

            var employee = await employeeRepository.GetByPersonnelNumberAsync("1000", CancellationToken.None);
            var monthlyRecord = await monthlyRecordRepository.GetOrCreateAsync(employee!.EmployeeId, 2026, 4, CancellationToken.None);
            var entry = Assert.Single(monthlyRecord.TimeEntries);

            Assert.Equal(1, result.ImportedCount);
            Assert.Equal(new DateOnly(2026, 4, 1), entry.WorkDate);
            Assert.Equal(12.5m, entry.HoursWorked);
            Assert.Equal(2m, entry.NightHours);
            Assert.True(await statusRepository.ExistsAsync(ImportConfigurationType.TimeData, 2026, 4, CancellationToken.None));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task IsMonthImported_RecognizesAlreadyImportedMonth()
    {
        var service = CreateImportService(
            new InMemoryEmployeeRepository(),
            new InMemoryEmployeeMonthlyRecordRepository(),
            new InMemoryImportExecutionStatusRepository(
                new ImportedMonthStatusDto(2026, 4, DateTimeOffset.UtcNow)));

        var exists = await service.IsMonthImportedAsync(ImportConfigurationType.TimeData, 2026, 4, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ImportTimeData_Overwrite_ReplacesExistingMonthData()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"time-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, "Personalnummer;Stunden\n1000;8\n");

        try
        {
            var employeeRepository = new InMemoryEmployeeRepository();
            await employeeRepository.SaveAsync(CreateEmployeeCommand(null, "1000", "Anna", "Aktiv", "Bern"), CancellationToken.None);
            var employee = await employeeRepository.GetByPersonnelNumberAsync("1000", CancellationToken.None);
            var monthlyRecordRepository = new InMemoryEmployeeMonthlyRecordRepository();
            var existingRecord = await monthlyRecordRepository.GetOrCreateAsync(employee!.EmployeeId, 2026, 5, CancellationToken.None);
            existingRecord.SaveTimeEntry(null, new DateOnly(2026, 5, 1), 3m, 0m, 0m, 0m, 0m, 0m, 0m, "alt");
            await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

            var statusRepository = new InMemoryImportExecutionStatusRepository(
                new ImportedMonthStatusDto(2026, 5, DateTimeOffset.UtcNow.AddDays(-2)));
            var service = CreateImportService(employeeRepository, monthlyRecordRepository, statusRepository);

            var result = await service.ImportTimeDataAsync(new ImportTimeDataCommand(
                filePath,
                ";",
                true,
                "\"",
                2026,
                5,
                true,
                BuildMappings(
                    ("personnel_number", "Personalnummer", false),
                    ("hours_worked", "Stunden", false))),
                CancellationToken.None);

            var updatedRecord = await monthlyRecordRepository.GetOrCreateAsync(employee.EmployeeId, 2026, 5, CancellationToken.None);
            var entry = Assert.Single(updatedRecord.TimeEntries);

            Assert.Equal(1, result.ImportedCount);
            Assert.Equal(8m, entry.HoursWorked);
            Assert.Null(entry.Note);
            Assert.True(await statusRepository.ExistsAsync(ImportConfigurationType.TimeData, 2026, 5, CancellationToken.None));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task DeleteImportedTimeMonth_RemovesTimeEntriesAndImportStatus()
    {
        var employeeRepository = new InMemoryEmployeeRepository();
        await employeeRepository.SaveAsync(CreateEmployeeCommand(null, "1000", "Anna", "Aktiv", "Bern"), CancellationToken.None);
        var employee = await employeeRepository.GetByPersonnelNumberAsync("1000", CancellationToken.None);
        var monthlyRecordRepository = new InMemoryEmployeeMonthlyRecordRepository();
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee!.EmployeeId, 2026, 6, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 6, 1), 7m, 0m, 0m, 0m, 0m, 0m, 0m, null);
        var statusRepository = new InMemoryImportExecutionStatusRepository(
            new ImportedMonthStatusDto(2026, 6, DateTimeOffset.UtcNow));
        var service = CreateImportService(employeeRepository, monthlyRecordRepository, statusRepository);

        await service.DeleteImportedTimeMonthAsync(2026, 6, CancellationToken.None);

        var updatedRecord = await monthlyRecordRepository.GetOrCreateAsync(employee.EmployeeId, 2026, 6, CancellationToken.None);
        Assert.Empty(updatedRecord.TimeEntries);
        Assert.False(await statusRepository.ExistsAsync(ImportConfigurationType.TimeData, 2026, 6, CancellationToken.None));
    }

    [Fact]
    public async Task ListImportedMonths_ReturnsSavedImportStatus()
    {
        var service = CreateImportService(
            new InMemoryEmployeeRepository(),
            new InMemoryEmployeeMonthlyRecordRepository(),
            new InMemoryImportExecutionStatusRepository(
                new ImportedMonthStatusDto(2026, 7, DateTimeOffset.UtcNow)));

        var months = await service.ListImportedMonthsAsync(ImportConfigurationType.TimeData, CancellationToken.None);

        var month = Assert.Single(months);
        Assert.Equal(2026, month.Year);
        Assert.Equal(7, month.Month);
    }

    private static ImportService CreateImportService(
        IEmployeeRepository employeeRepository,
        IEmployeeMonthlyRecordRepository? monthlyRecordRepository = null,
        IImportExecutionStatusRepository? importExecutionStatusRepository = null)
    {
        return new ImportService(
            new InMemoryImportMappingConfigurationRepository(),
            new CsvImportFileReader(),
            employeeRepository,
            monthlyRecordRepository ?? new InMemoryEmployeeMonthlyRecordRepository(),
            importExecutionStatusRepository ?? new InMemoryImportExecutionStatusRepository());
    }

    private static IReadOnlyCollection<ImportFieldMappingDto> BuildMappings(params (string FieldKey, string CsvColumnName, bool AllowEmpty)[] items)
    {
        return items
            .Select(item => new ImportFieldMappingDto(item.FieldKey, item.CsvColumnName, item.AllowEmpty))
            .ToArray();
    }

    private static SaveEmployeeCommand CreateEmployeeCommand(
        Guid? employeeId,
        string personnelNumber,
        string firstName,
        string lastName,
        string city,
        string? phoneNumber = null)
    {
        return new SaveEmployeeCommand(
            employeeId,
            personnelNumber,
            firstName,
            lastName,
            new DateOnly(1990, 1, 1),
            new DateOnly(2025, 1, 1),
            null,
            true,
            "Beispielstrasse",
            "1",
            null,
            "8000",
            city,
            "Schweiz",
            "Schweiz",
            "CH",
            "B",
            "Ordentlich",
            false,
            "756.0000.0000.00",
            "CH9300762011623852957",
            phoneNumber,
            "test@example.ch",
            null,
            null,
            null,
            EmployeeWageType.Hourly,
            new DateOnly(2025, 1, 1),
            null,
            0m,
            0m,
            0m);
    }

    private sealed class InMemoryImportMappingConfigurationRepository : IImportMappingConfigurationRepository
    {
        private readonly Dictionary<Guid, ImportConfigurationDto> _items = [];

        public Task<IReadOnlyCollection<ImportConfigurationListItemDto>> ListAsync(ImportConfigurationType type, CancellationToken cancellationToken)
        {
            var result = _items.Values
                .Where(item => item.Type == type)
                .Select(item => new ImportConfigurationListItemDto(item.ConfigurationId, item.Name))
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<ImportConfigurationListItemDto>>(result);
        }

        public Task<ImportConfigurationDto?> GetByIdAsync(Guid configurationId, CancellationToken cancellationToken)
        {
            _items.TryGetValue(configurationId, out var item);
            return Task.FromResult(item);
        }

        public Task<ImportConfigurationDto> SaveAsync(SaveImportConfigurationCommand command, CancellationToken cancellationToken)
        {
            var id = command.ConfigurationId ?? Guid.NewGuid();
            var item = new ImportConfigurationDto(id, command.Type, command.Name, command.Delimiter, command.FieldsEnclosed, command.TextQualifier, command.Mappings);
            _items[id] = item;
            return Task.FromResult(item);
        }
    }

    private sealed class InMemoryEmployeeRepository : IEmployeeRepository
    {
        private readonly Dictionary<Guid, EmployeeDetailsDto> _employees = [];

        public Task<IReadOnlyCollection<EmployeeListItemDto>> ListAsync(EmployeeListQuery query, CancellationToken cancellationToken)
        {
            var result = _employees.Values
                .OrderBy(item => item.LastName)
                .ThenBy(item => item.FirstName)
                .Select(item => new EmployeeListItemDto(
                    item.EmployeeId,
                    item.PersonnelNumber,
                    $"{item.FirstName} {item.LastName}",
                    item.IsActive,
                    item.City,
                    item.Country,
                    item.Email,
                    item.DepartmentName,
                    item.EmploymentCategoryName,
                    item.EmploymentLocationName,
                    item.HourlyRateChf,
                    item.MonthlyBvgDeductionChf,
                    item.ContractValidFrom,
                    item.ContractValidTo))
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<EmployeeListItemDto>>(result);
        }

        public Task<EmployeeDetailsDto?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            _employees.TryGetValue(employeeId, out var employee);
            return Task.FromResult(employee);
        }

        public Task<EmployeeDetailsDto?> GetByPersonnelNumberAsync(string personnelNumber, CancellationToken cancellationToken)
        {
            var employee = _employees.Values.SingleOrDefault(item => item.PersonnelNumber == personnelNumber.Trim());
            return Task.FromResult(employee);
        }

        public Task<bool> PersonnelNumberExistsAsync(string personnelNumber, Guid? excludingEmployeeId, CancellationToken cancellationToken)
        {
            var trimmedPersonnelNumber = personnelNumber.Trim();
            var exists = _employees.Values.Any(item =>
                item.PersonnelNumber == trimmedPersonnelNumber
                && (!excludingEmployeeId.HasValue || item.EmployeeId != excludingEmployeeId.Value));
            return Task.FromResult(exists);
        }

        public Task ArchiveAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<EmployeeDetailsDto> SaveAsync(SaveEmployeeCommand command, CancellationToken cancellationToken)
        {
            var employeeId = command.EmployeeId ?? Guid.NewGuid();
            var employee = new EmployeeDetailsDto(
                employeeId,
                command.PersonnelNumber,
                command.FirstName,
                command.LastName,
                command.BirthDate,
                command.EntryDate,
                command.ExitDate,
                command.IsActive,
                command.Street,
                command.HouseNumber,
                command.AddressLine2,
                command.PostalCode,
                command.City,
                command.Country,
                command.ResidenceCountry,
                command.Nationality,
                command.PermitCode,
                command.TaxStatus,
                command.IsSubjectToWithholdingTax,
                command.AhvNumber,
                command.Iban,
                command.PhoneNumber,
                command.Email,
                command.DepartmentOptionId,
                null,
                command.EmploymentCategoryOptionId,
                null,
                command.EmploymentLocationOptionId,
                null,
                command.WageType,
                command.ContractValidFrom,
                command.ContractValidTo,
                command.HourlyRateChf,
                command.MonthlyBvgDeductionChf,
                command.SpecialSupplementRateChf);

            _employees[employeeId] = employee;
            return Task.FromResult(employee);
        }
    }

    private sealed class InMemoryEmployeeMonthlyRecordRepository : IEmployeeMonthlyRecordRepository
    {
        private readonly Dictionary<(Guid EmployeeId, int Year, int Month), EmployeeMonthlyRecord> _records = [];

        public Task<EmployeeMonthlyRecord> GetOrCreateAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
        {
            var key = (employeeId, year, month);
            if (!_records.TryGetValue(key, out var record))
            {
                record = new EmployeeMonthlyRecord(employeeId, year, month);
                _records[key] = record;
            }

            return Task.FromResult(record);
        }

        public Task<EmployeeMonthlyRecord?> GetByIdAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            var record = _records.Values.SingleOrDefault(item => item.Id == monthlyRecordId);
            return Task.FromResult<EmployeeMonthlyRecord?>(record);
        }

        public Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<MonthlyRecordDetailsDto?>(null);
        }

        public Task<IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>> ListTimeCaptureOverviewAsync(int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>>([]);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteTimeEntriesForMonthAsync(int year, int month, CancellationToken cancellationToken)
        {
            foreach (var record in _records.Values.Where(item => item.Year == year && item.Month == month).ToArray())
            {
                foreach (var timeEntry in record.TimeEntries.ToArray())
                {
                    record.RemoveTimeEntry(timeEntry.Id);
                }
            }

            return Task.CompletedTask;
        }

        public void ClearTracking()
        {
        }

        public void MarkAsAdded<TEntity>(TEntity entity) where TEntity : class
        {
        }
    }

    private sealed class InMemoryImportExecutionStatusRepository : IImportExecutionStatusRepository
    {
        private readonly Dictionary<(ImportConfigurationType Type, int Year, int Month), ImportedMonthStatusDto> _items = [];

        public InMemoryImportExecutionStatusRepository(params ImportedMonthStatusDto[] items)
        {
            foreach (var item in items)
            {
                _items[(ImportConfigurationType.TimeData, item.Year, item.Month)] = item;
            }
        }

        public Task<bool> ExistsAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult(_items.ContainsKey((type, year, month)));
        }

        public Task MarkImportedAsync(ImportConfigurationType type, int year, int month, DateTimeOffset importedAtUtc, CancellationToken cancellationToken)
        {
            _items[(type, year, month)] = new ImportedMonthStatusDto(year, month, importedAtUtc);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken)
        {
            _items.Remove((type, year, month));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ImportedMonthStatusDto>> ListAsync(ImportConfigurationType type, CancellationToken cancellationToken)
        {
            var items = _items
                .Where(item => item.Key.Type == type)
                .Select(item => item.Value)
                .OrderByDescending(item => item.Year)
                .ThenByDescending(item => item.Month)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<ImportedMonthStatusDto>>(items);
        }
    }
}
