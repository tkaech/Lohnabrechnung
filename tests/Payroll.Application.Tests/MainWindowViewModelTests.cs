using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Settings;
using Payroll.Desktop.ViewModels;
using Payroll.Domain.Employees;
using Payroll.Domain.MonthlyRecords;

namespace Payroll.Application.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task InitializeAsync_SelectsFirstEmployeeAndLoadsDetails()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();

        await WaitUntilAsync(() =>
            viewModel.PersonnelNumber == "1000"
            && viewModel.SelectedEmployee?.EmployeeId == employee.EmployeeId
            && viewModel.FirstName == "Anna"
            && viewModel.LastName == "Aktiv");

        Assert.Equal("Anna", viewModel.FirstName);
        Assert.Equal("Aktiv", viewModel.LastName);
        Assert.Contains("geladen", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectedEmployee_LoadsDifferentEmployeeAfterSelectionChange()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var repository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.SelectedEmployee = viewModel.Employees.Single(item => item.EmployeeId == secondEmployee.EmployeeId);
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1001" && viewModel.FirstName == "Bruno");

        Assert.Equal(secondEmployee.EmployeeId, viewModel.SelectedEmployee?.EmployeeId);
        Assert.Equal("Bruno", viewModel.FirstName);
        Assert.Equal("Bereit", viewModel.LastName);
        Assert.Contains("geladen", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelCommand_AfterCreatingNewEmployee_RestoresPreviousSelection()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var repository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.SelectedEmployee = viewModel.Employees.Single(item => item.EmployeeId == secondEmployee.EmployeeId);
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1001");

        viewModel.NewEmployeeCommand.Execute(null);
        Assert.True(viewModel.CanCancel);

        viewModel.CancelCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy && viewModel.PersonnelNumber == "1001");

        Assert.Equal(secondEmployee.EmployeeId, viewModel.SelectedEmployee?.EmployeeId);
        Assert.False(viewModel.CanSave);
        Assert.Equal("Neueingabe verworfen.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ConfirmDeleteCommand_ArchivesEmployeeAndReloadsCurrentSelection()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        Assert.False(viewModel.CanRequestDelete);
        viewModel.DeleteCommand.Execute(null);
        Assert.False(viewModel.ShowDeleteConfirmation);

        viewModel.EditEmployeeCommand.Execute(null);
        Assert.True(viewModel.CanRequestDelete);

        viewModel.DeleteCommand.Execute(null);
        Assert.True(viewModel.ShowDeleteConfirmation);
        Assert.True(viewModel.ConfirmDeleteCommand.CanExecute(null));
        Assert.True(viewModel.DismissDeleteCommand.CanExecute(null));

        viewModel.ConfirmDeleteCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy && viewModel.StatusMessage == "Mitarbeitender archiviert und auf inaktiv gesetzt.");

        Assert.False(viewModel.IsActiveEmployee);
        Assert.Equal(employee.EmployeeId, viewModel.SelectedEmployee?.EmployeeId);
        Assert.Equal("Inaktiv", viewModel.SelectedEmployee?.StatusSummary);
    }

    [Fact]
    public async Task SearchRemainsAvailableInEditMode()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.EditEmployeeCommand.Execute(null);

        Assert.True(viewModel.CanSearchEmployees);
        Assert.True(viewModel.SearchCommand.CanExecute(null));
        Assert.True(viewModel.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public async Task SearchBecomesAvailableAgainAfterBusyInitialization()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        repository.GetByIdDelay = _ => repository.FirstLoadRelease.Task;
        var viewModel = CreateViewModel(repository);

        var initializeTask = viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.IsBusy);

        Assert.False(viewModel.CanSearchEmployees);

        repository.FirstLoadRelease.SetResult();
        await initializeTask;
        await WaitUntilAsync(() => !viewModel.IsBusy);

        Assert.True(viewModel.CanSearchEmployees);
        Assert.True(viewModel.SearchCommand.CanExecute(null));
        Assert.True(viewModel.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public void WorkspaceDefaultsToTimeAndExpenses_AndCanSwitchToEmployees()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        Assert.True(viewModel.IsTimeAndExpensesWorkspace);
        Assert.False(viewModel.IsEmployeeWorkspace);

        viewModel.ShowEmployeesCommand.Execute(null);

        Assert.True(viewModel.IsEmployeeWorkspace);
        Assert.False(viewModel.IsTimeAndExpensesWorkspace);

        viewModel.ShowTimeAndExpensesCommand.Execute(null);

        Assert.True(viewModel.IsTimeAndExpensesWorkspace);
        Assert.False(viewModel.IsEmployeeWorkspace);
    }

    [Fact]
    public async Task SettingsWorkspace_LoadsAndSavesCentralSupplementRates()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var employeeRepository = new TestEmployeeRepository(employee);
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            new PayrollSettingsService(settingsRepository),
            new MonthlyRecordViewModel(new MonthlyRecordService(new InMemoryMonthlyRecordRepository())),
            "Test");

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.SettingsNightSupplementRate == "0.25");

        viewModel.ShowSettingsCommand.Execute(null);
        viewModel.SettingsNightSupplementRate = "0.30";
        viewModel.SettingsSundaySupplementRate = "0.60";
        viewModel.SettingsHolidaySupplementRate = "1.10";
        viewModel.SaveSettingsCommand.Execute(null);

        await WaitUntilAsync(() => viewModel.StatusMessage == "Einstellungen gespeichert.");

        Assert.True(viewModel.IsSettingsWorkspace);
        Assert.Equal(0.30m, settingsRepository.Current.NightSupplementRate);
        Assert.Equal(0.60m, settingsRepository.Current.SundaySupplementRate);
        Assert.Equal(1.10m, settingsRepository.Current.HolidaySupplementRate);
    }

    private static MainWindowViewModel CreateViewModel(TestEmployeeRepository repository)
    {
        return new MainWindowViewModel(
            new EmployeeService(repository),
            new PayrollSettingsService(new InMemoryPayrollSettingsRepository()),
            new MonthlyRecordViewModel(new MonthlyRecordService(new InMemoryMonthlyRecordRepository())),
            "Test");
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var startedAt = DateTime.UtcNow;

        while (!condition())
        {
            if ((DateTime.UtcNow - startedAt).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Condition was not met within the expected time.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class TestEmployeeRepository : IEmployeeRepository
    {
        private readonly Dictionary<Guid, EmployeeDetailsDto> _employees;

        public TestEmployeeRepository(params EmployeeDetailsDto[] employees)
        {
            _employees = employees.ToDictionary(employee => employee.EmployeeId);
            FirstLoadRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Func<Guid, Task>? GetByIdDelay { get; set; }
        public TaskCompletionSource FirstLoadRelease { get; }

        public Task<IReadOnlyCollection<EmployeeListItemDto>> ListAsync(EmployeeListQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<EmployeeDetailsDto> items = _employees.Values;

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var searchText = query.SearchText.Trim();
                items = items.Where(item =>
                    item.PersonnelNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || item.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || item.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || item.City.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || (item.Email?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (query.IsActive.HasValue)
            {
                items = items.Where(item => item.IsActive == query.IsActive.Value);
            }

            var result = items
                .OrderBy(item => item.LastName)
                .ThenBy(item => item.FirstName)
                .Select(ToListItem)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<EmployeeListItemDto>>(result);
        }

        public async Task<EmployeeDetailsDto?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            if (GetByIdDelay is not null)
            {
                await GetByIdDelay(employeeId);
            }

            _employees.TryGetValue(employeeId, out var employee);
            return employee;
        }

        public Task<bool> PersonnelNumberExistsAsync(string personnelNumber, Guid? excludingEmployeeId, CancellationToken cancellationToken)
        {
            var trimmedPersonnelNumber = personnelNumber.Trim();
            var result = _employees.Values.Any(item =>
                item.PersonnelNumber == trimmedPersonnelNumber
                && (!excludingEmployeeId.HasValue || item.EmployeeId != excludingEmployeeId.Value));

            return Task.FromResult(result);
        }

        public Task ArchiveAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            if (_employees.TryGetValue(employeeId, out var employee))
            {
                _employees[employeeId] = employee with
                {
                    IsActive = false,
                    ExitDate = employee.ExitDate ?? DateOnly.FromDateTime(DateTime.Today)
                };
            }

            return Task.CompletedTask;
        }

        public Task<EmployeeDetailsDto> SaveAsync(SaveEmployeeCommand command, CancellationToken cancellationToken)
        {
            var employeeId = command.EmployeeId ?? Guid.NewGuid();
            var employee = new EmployeeDetailsDto(
                employeeId,
                command.PersonnelNumber.Trim(),
                command.FirstName.Trim(),
                command.LastName.Trim(),
                command.BirthDate,
                command.EntryDate,
                command.ExitDate,
                command.IsActive,
                command.Street.Trim(),
                command.HouseNumber?.Trim(),
                command.AddressLine2?.Trim(),
                command.PostalCode.Trim(),
                command.City.Trim(),
                command.Country.Trim(),
                command.ResidenceCountry?.Trim(),
                command.Nationality?.Trim(),
                command.PermitCode?.Trim(),
                command.TaxStatus?.Trim(),
                command.IsSubjectToWithholdingTax,
                command.AhvNumber?.Trim(),
                command.Iban?.Trim(),
                command.PhoneNumber?.Trim(),
                command.Email?.Trim(),
                command.ContractValidFrom,
                command.ContractValidTo,
                command.HourlyRateChf,
                command.MonthlyBvgDeductionChf);

            _employees[employeeId] = employee;
            return Task.FromResult(employee);
        }

        public static EmployeeDetailsDto CreateDetails(string personnelNumber, string firstName, string lastName, string city)
        {
            return new EmployeeDetailsDto(
                Guid.NewGuid(),
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
                "+41 79 000 00 00",
                $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.ch",
                new DateOnly(2025, 1, 1),
                null,
                32.5m,
                280m);
        }

        private static EmployeeListItemDto ToListItem(EmployeeDetailsDto employee)
        {
            return new EmployeeListItemDto(
                employee.EmployeeId,
                employee.PersonnelNumber,
                $"{employee.FirstName} {employee.LastName}",
                employee.IsActive,
                employee.City,
                employee.Country,
                employee.Email,
                employee.HourlyRateChf,
                employee.MonthlyBvgDeductionChf,
                employee.ContractValidFrom,
                employee.ContractValidTo);
        }
    }

    private sealed class InMemoryMonthlyRecordRepository : IEmployeeMonthlyRecordRepository
    {
        public Task<EmployeeMonthlyRecord> GetOrCreateAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmployeeMonthlyRecord(employeeId, year, month));
        }

        public Task<EmployeeMonthlyRecord?> GetByIdAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<EmployeeMonthlyRecord?>(null);
        }

        public Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<MonthlyRecordDetailsDto?>(null);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void ClearTracking()
        {
        }

        public void MarkAsAdded<TEntity>(TEntity entity) where TEntity : class
        {
        }
    }

    private sealed class InMemoryPayrollSettingsRepository : IPayrollSettingsRepository
    {
        private PayrollSettingsDto _settings = new(0.25m, 0.50m, 1.00m);

        public PayrollSettingsDto Current => _settings;

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
