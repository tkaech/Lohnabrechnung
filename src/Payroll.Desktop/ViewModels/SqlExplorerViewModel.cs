using System.Collections.ObjectModel;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Desktop.ViewModels;

public sealed class SqlExplorerViewModel : ViewModelBase
{
    private readonly PayrollDbContext? _dbContext;
    private readonly Dictionary<string, SqlExplorerTablePolicy> _policyByTableName = new(StringComparer.Ordinal)
    {
        ["__EFMigrationsHistory"] = SqlExplorerTablePolicy.Hidden,
        ["Employees"] = SqlExplorerTablePolicy.VisibleReadOnly,
        ["EmploymentContracts"] = SqlExplorerTablePolicy.CriticalReadOnly,
        ["EmployeeMonthlyRecords"] = SqlExplorerTablePolicy.CriticalReadOnly,
        ["TimeEntries"] = SqlExplorerTablePolicy.CriticalReadOnly,
        ["ExpenseEntries"] = SqlExplorerTablePolicy.CriticalReadOnly,
        ["PayrollSettings"] = SqlExplorerTablePolicy.CriticalReadOnly,
        ["PayrollGeneralSettingsVersions"] = SqlExplorerTablePolicy.CriticalReadOnly,
        ["PayrollHourlySettingsVersions"] = SqlExplorerTablePolicy.CriticalReadOnly,
        ["PayrollMonthlySalarySettingsVersions"] = SqlExplorerTablePolicy.CriticalReadOnly,
        ["DepartmentOptions"] = SqlExplorerTablePolicy.Editable,
        ["EmploymentCategoryOptions"] = SqlExplorerTablePolicy.Editable,
        ["EmploymentLocationOptions"] = SqlExplorerTablePolicy.Editable,
        ["ImportMappingConfigurations"] = SqlExplorerTablePolicy.VisibleReadOnly,
        ["ImportExecutionStatuses"] = SqlExplorerTablePolicy.VisibleReadOnly
    };

    private SqlExplorerTableItemViewModel? _selectedTable;
    private SqlExplorerRowViewModel? _selectedRow;
    private int _selectedLimit = 100;
    private string _statusMessage = "Tabelle auswaehlen.";
    private bool _isInitialized;
    private bool _isLoading;

    public SqlExplorerViewModel(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
        Tables = [];
        Columns = [];
        Rows = [];
        SelectedRowDetails = [];
        LimitOptions = [100, 500, 1000];
        ReloadCommand = new DelegateCommand(ReloadAsync, () => SelectedTable is not null && !_isLoading);
    }

    public SqlExplorerViewModel()
    {
        Tables = [];
        Columns = [];
        Rows = [];
        SelectedRowDetails = [];
        LimitOptions = [100, 500, 1000];
        ReloadCommand = new DelegateCommand(ReloadAsync, () => SelectedTable is not null && !_isLoading);
        StatusMessage = "Keine SQL-Datenquelle verbunden.";
    }

    public ObservableCollection<SqlExplorerTableItemViewModel> Tables { get; }
    public ObservableCollection<SqlExplorerColumnViewModel> Columns { get; }
    public ObservableCollection<SqlExplorerRowViewModel> Rows { get; }
    public ObservableCollection<SqlExplorerDetailItemViewModel> SelectedRowDetails { get; }
    public IReadOnlyList<int> LimitOptions { get; }
    public DelegateCommand ReloadCommand { get; }

    public SqlExplorerTableItemViewModel? SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (!SetProperty(ref _selectedTable, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(SelectedTablePolicyHint));
            RaisePropertyChanged(nameof(ShowCriticalPolicyWarning));
            SelectedRow = null;

            if (_isInitialized && value is not null)
            {
                _ = LoadSelectedTableAsync();
            }
            else
            {
                Columns.Clear();
                Rows.Clear();
                SelectedRowDetails.Clear();
            }

            ReloadCommand.RaiseCanExecuteChanged();
        }
    }

    public SqlExplorerRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (!SetProperty(ref _selectedRow, value))
            {
                return;
            }

            ApplySelectedRowDetails(value);
        }
    }

    public int SelectedLimit
    {
        get => _selectedLimit;
        set
        {
            if (!SetProperty(ref _selectedLimit, value))
            {
                return;
            }

            if (_isInitialized && SelectedTable is not null)
            {
                _ = LoadSelectedTableAsync();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedTablePolicyHint => SelectedTable?.Policy switch
    {
        SqlExplorerTablePolicy.CriticalReadOnly => "Kritische Tabelle. Nur lesend anzeigen, weil Historisierung, Snapshots oder Berechnung davon abhaengen.",
        SqlExplorerTablePolicy.Editable => "Fuer spaetere Bearbeitung freigegeben, in Schritt 1 aber weiterhin nur read-only.",
        SqlExplorerTablePolicy.VisibleReadOnly => "Freigegebene Tabelle im read-only Explorer.",
        _ => "Tabelle auswaehlen."
    };

    public bool ShowCriticalPolicyWarning => SelectedTable?.Policy == SqlExplorerTablePolicy.CriticalReadOnly;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_dbContext is null)
        {
            _isInitialized = true;
            return;
        }

        await LoadTablesAsync();
        _isInitialized = true;

        if (SelectedTable is null && Tables.Count > 0)
        {
            SelectedTable = Tables[0];
            return;
        }

        if (SelectedTable is not null)
        {
            await LoadSelectedTableAsync();
        }
    }

    private async Task ReloadAsync()
    {
        await LoadSelectedTableAsync();
    }

    private async Task LoadTablesAsync()
    {
        if (_dbContext is null)
        {
            return;
        }

        Tables.Clear();

        var entityTables = _dbContext.Model.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .Where(tableName => !string.IsNullOrWhiteSpace(tableName))
            .Distinct(StringComparer.Ordinal)
            .Select(tableName => tableName!)
            .ToList();

        var visibleTables = entityTables
            .Select(tableName => new SqlExplorerTableItemViewModel(
                tableName,
                tableName,
                _policyByTableName.TryGetValue(tableName, out var policy)
                    ? policy
                    : SqlExplorerTablePolicy.VisibleReadOnly))
            .Where(table => table.Policy != SqlExplorerTablePolicy.Hidden)
            .OrderBy(table => GetPolicyOrder(table.Policy))
            .ThenBy(table => table.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var table in visibleTables)
        {
            Tables.Add(table);
        }

        StatusMessage = Tables.Count == 0
            ? "Keine freigegebenen Tabellen gefunden."
            : $"{Tables.Count} Tabellen verfuegbar.";

        await Task.CompletedTask;
    }

    private async Task LoadSelectedTableAsync()
    {
        if (_dbContext is null || SelectedTable is null || _isLoading)
        {
            return;
        }

        _isLoading = true;
        ReloadCommand.RaiseCanExecuteChanged();

        try
        {
            var activeFilters = Columns
                .Where(column => !string.IsNullOrWhiteSpace(column.FilterValue))
                .Select(column => new KeyValuePair<string, string>(column.ColumnName, column.FilterValue))
                .ToList();

            Columns.Clear();
            Rows.Clear();
            SelectedRowDetails.Clear();
            SelectedRow = null;

            var connection = _dbContext.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != ConnectionState.Open;
            if (shouldCloseConnection)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = BuildSelectSql(SelectedTable.TableName, activeFilters);

                foreach (var filter in activeFilters.Select((entry, index) => new { entry, index }))
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"$filter{filter.index}";
                    parameter.Value = $"%{filter.entry.Value}%";
                    command.Parameters.Add(parameter);
                }

                await using var reader = await command.ExecuteReaderAsync();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var filterValue = activeFilters.FirstOrDefault(entry => string.Equals(entry.Key, reader.GetName(i), StringComparison.Ordinal)).Value;
                    Columns.Add(new SqlExplorerColumnViewModel(
                        reader.GetName(i),
                        reader.GetName(i),
                        reader.GetDataTypeName(i),
                        Math.Clamp(reader.GetName(i).Length * 12, 140, 280),
                        filterValue));
                }

                while (await reader.ReadAsync())
                {
                    var cells = new List<SqlExplorerCellViewModel>(reader.FieldCount);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? string.Empty : Convert.ToString(reader.GetValue(i)) ?? string.Empty;
                        cells.Add(new SqlExplorerCellViewModel(Columns[i].Width, value));
                    }

                    Rows.Add(new SqlExplorerRowViewModel(cells));
                }
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }

            StatusMessage = $"{Rows.Count} Zeilen aus {SelectedTable.DisplayName} geladen.";
            SelectedRow = Rows.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Columns.Clear();
            Rows.Clear();
            SelectedRowDetails.Clear();
            StatusMessage = $"Tabelle konnte nicht geladen werden: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            ReloadCommand.RaiseCanExecuteChanged();
        }
    }

    private string BuildSelectSql(string tableName, IReadOnlyList<KeyValuePair<string, string>> activeFilters)
    {
        var whereClauses = activeFilters
            .Select((entry, index) => $"CAST(\"{EscapeIdentifier(entry.Key)}\" AS TEXT) LIKE $filter{index}")
            .ToList();

        var whereClause = whereClauses.Count == 0
            ? string.Empty
            : $" WHERE {string.Join(" AND ", whereClauses)}";

        return $"SELECT * FROM \"{EscapeIdentifier(tableName)}\"{whereClause} LIMIT {SelectedLimit};";
    }

    private void ApplySelectedRowDetails(SqlExplorerRowViewModel? row)
    {
        SelectedRowDetails.Clear();
        if (row is null)
        {
            return;
        }

        for (var i = 0; i < Columns.Count && i < row.Cells.Count; i++)
        {
            SelectedRowDetails.Add(new SqlExplorerDetailItemViewModel(Columns[i].DisplayName, row.Cells[i].Value));
        }
    }

    private static int GetPolicyOrder(SqlExplorerTablePolicy policy) => policy switch
    {
        SqlExplorerTablePolicy.VisibleReadOnly => 0,
        SqlExplorerTablePolicy.Editable => 1,
        SqlExplorerTablePolicy.CriticalReadOnly => 2,
        _ => 3
    };

    private static string EscapeIdentifier(string identifier) => identifier.Replace("\"", "\"\"", StringComparison.Ordinal);
}

public sealed class SqlExplorerTableItemViewModel(string tableName, string displayName, SqlExplorerTablePolicy policy)
{
    public string TableName { get; } = tableName;
    public string DisplayName { get; } = displayName;
    public SqlExplorerTablePolicy Policy { get; } = policy;

    public string PolicyDisplay => Policy switch
    {
        SqlExplorerTablePolicy.VisibleReadOnly => "Visible read-only",
        SqlExplorerTablePolicy.CriticalReadOnly => "Critical read-only",
        SqlExplorerTablePolicy.Editable => "Editable (in Schritt 1 read-only)",
        _ => "Hidden"
    };
}

public sealed class SqlExplorerColumnViewModel(string columnName, string displayName, string columnHint, int width, string? initialFilterValue = null) : ViewModelBase
{
    private string _filterValue = initialFilterValue ?? string.Empty;

    public string ColumnName { get; } = columnName;
    public string DisplayName { get; } = displayName;
    public string ColumnHint { get; } = columnHint;
    public int Width { get; } = width;
    public string FilterWatermark => "Filter";

    public string FilterValue
    {
        get => _filterValue;
        set => SetProperty(ref _filterValue, value);
    }
}

public sealed class SqlExplorerRowViewModel(IReadOnlyList<SqlExplorerCellViewModel> cells)
{
    public IReadOnlyList<SqlExplorerCellViewModel> Cells { get; } = cells;
}

public sealed class SqlExplorerCellViewModel(int width, string value)
{
    public int Width { get; } = width;
    public string Value { get; } = value;
}

public sealed class SqlExplorerDetailItemViewModel(string label, string value)
{
    public string Label { get; } = label;
    public string Value { get; } = value;
}

public enum SqlExplorerTablePolicy
{
    Hidden,
    VisibleReadOnly,
    CriticalReadOnly,
    Editable
}
