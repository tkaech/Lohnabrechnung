using System.Collections.Specialized;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Payroll.Desktop.Behaviors;

public sealed class PersistentDataGridState
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<PersistentDataGridState, DataGrid, bool>("IsEnabled");

    public static readonly AttachedProperty<string?> GridKeyProperty =
        AvaloniaProperty.RegisterAttached<PersistentDataGridState, DataGrid, string?>("GridKey");

    public static readonly AttachedProperty<string?> ColumnKeyProperty =
        AvaloniaProperty.RegisterAttached<PersistentDataGridState, DataGridColumn, string?>("ColumnKey");

    private static readonly AttachedProperty<DataGridStateController?> ControllerProperty =
        AvaloniaProperty.RegisterAttached<PersistentDataGridState, DataGrid, DataGridStateController?>("Controller");

    static PersistentDataGridState()
    {
        IsEnabledProperty.Changed.AddClassHandler<DataGrid>(OnBehaviorPropertyChanged);
        GridKeyProperty.Changed.AddClassHandler<DataGrid>(OnBehaviorPropertyChanged);
    }

    private PersistentDataGridState()
    {
    }

    public static bool GetIsEnabled(DataGrid element) => element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DataGrid element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static string? GetGridKey(DataGrid element) => element.GetValue(GridKeyProperty);

    public static void SetGridKey(DataGrid element, string? value) => element.SetValue(GridKeyProperty, value);

    public static string? GetColumnKey(DataGridColumn element) => element.GetValue(ColumnKeyProperty);

    public static void SetColumnKey(DataGridColumn element, string? value) => element.SetValue(ColumnKeyProperty, value);

    private static void OnBehaviorPropertyChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs e)
    {
        var existingController = grid.GetValue(ControllerProperty);
        var isEnabled = GetIsEnabled(grid);
        var gridKey = GetGridKey(grid);

        if (!isEnabled || string.IsNullOrWhiteSpace(gridKey))
        {
            existingController?.Dispose();
            grid.ClearValue(ControllerProperty);
            return;
        }

        if (existingController is not null)
        {
            existingController.UpdateGridKey(gridKey!);
            return;
        }

        var controller = new DataGridStateController(grid, gridKey!);
        grid.SetValue(ControllerProperty, controller);
    }

    private sealed class DataGridStateController : IDisposable
    {
        private readonly DataGrid _grid;
        private INotifyCollectionChanged? _columnsCollection;
        private bool _isApplyingLayout;
        private bool _isDisposed;
        private string _gridKey;

        public DataGridStateController(DataGrid grid, string gridKey)
        {
            _grid = grid;
            _gridKey = gridKey;
            _grid.AttachedToVisualTree += OnAttachedToVisualTree;
            _grid.DetachedFromVisualTree += OnDetachedFromVisualTree;
            _grid.ColumnDisplayIndexChanged += OnGridLayoutChanged;
            _grid.ColumnReordered += OnGridLayoutChanged;
            AttachColumnSubscriptions();
            ScheduleApplyLayout();
        }

        public void UpdateGridKey(string gridKey)
        {
            if (string.Equals(_gridKey, gridKey, StringComparison.Ordinal))
            {
                return;
            }

            _gridKey = gridKey;
            ScheduleApplyLayout();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _grid.AttachedToVisualTree -= OnAttachedToVisualTree;
            _grid.DetachedFromVisualTree -= OnDetachedFromVisualTree;
            _grid.ColumnDisplayIndexChanged -= OnGridLayoutChanged;
            _grid.ColumnReordered -= OnGridLayoutChanged;
            DetachColumnSubscriptions();
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            ScheduleApplyLayout();
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            SaveLayout();
        }

        private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DetachColumnSubscriptions();
            AttachColumnSubscriptions();
            ScheduleApplyLayout();
        }

        private void OnColumnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_isApplyingLayout || _isDisposed || e.Property != DataGridColumn.WidthProperty)
            {
                return;
            }

            SaveLayout();
        }

        private void OnGridLayoutChanged(object? sender, DataGridColumnEventArgs e)
        {
            if (_isApplyingLayout || _isDisposed)
            {
                return;
            }

            SaveLayout();
        }

        private void AttachColumnSubscriptions()
        {
            if (_grid.Columns is INotifyCollectionChanged columnsCollection)
            {
                _columnsCollection = columnsCollection;
                _columnsCollection.CollectionChanged += OnColumnsCollectionChanged;
            }

            foreach (var column in _grid.Columns)
            {
                column.PropertyChanged += OnColumnPropertyChanged;
            }
        }

        private void DetachColumnSubscriptions()
        {
            if (_columnsCollection is not null)
            {
                _columnsCollection.CollectionChanged -= OnColumnsCollectionChanged;
                _columnsCollection = null;
            }

            foreach (var column in _grid.Columns)
            {
                column.PropertyChanged -= OnColumnPropertyChanged;
            }
        }

        private void ScheduleApplyLayout()
        {
            if (_isDisposed)
            {
                return;
            }

            Dispatcher.UIThread.Post(ApplyLayout, DispatcherPriority.Loaded);
        }

        private void ApplyLayout()
        {
            if (_isDisposed || string.IsNullOrWhiteSpace(_gridKey) || _grid.Columns.Count == 0)
            {
                return;
            }

            var layout = DataGridLayoutStore.Load(_gridKey);
            if (layout is null || layout.Columns.Count == 0)
            {
                ApplyGridDefaults();
                return;
            }

            var columnsByKey = _grid.Columns
                .Select((column, index) => new ColumnReference(column, index, ResolveColumnKey(column)))
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .ToDictionary(item => item.Key!, StringComparer.Ordinal);

            _isApplyingLayout = true;
            try
            {
                var rankedColumns = _grid.Columns
                    .Select((column, index) =>
                    {
                        var key = ResolveColumnKey(column);
                        var savedColumn = key is not null
                            ? layout.Columns.FirstOrDefault(item => string.Equals(item.ColumnKey, key, StringComparison.Ordinal))
                            : null;
                        return new RankedColumn(column, index, savedColumn?.DisplayIndex ?? int.MaxValue, savedColumn?.Width);
                    })
                    .OrderBy(item => item.DisplayIndex)
                    .ThenBy(item => item.OriginalIndex)
                    .ToList();

                for (var index = 0; index < rankedColumns.Count; index++)
                {
                    if (rankedColumns[index].Column.DisplayIndex != index)
                    {
                        rankedColumns[index].Column.DisplayIndex = index;
                    }
                }

                foreach (var savedColumn in layout.Columns)
                {
                    if (!columnsByKey.TryGetValue(savedColumn.ColumnKey, out var columnReference))
                    {
                        continue;
                    }

                    if (savedColumn.Width > 0d)
                    {
                        columnReference.Column.Width = new DataGridLength(savedColumn.Width);
                    }
                }

                ApplyGridDefaults();
            }
            finally
            {
                _isApplyingLayout = false;
            }
        }

        private void ApplyGridDefaults()
        {
            _grid.CanUserReorderColumns = true;
            _grid.CanUserResizeColumns = true;
        }

        private void SaveLayout()
        {
            if (_isDisposed || string.IsNullOrWhiteSpace(_gridKey) || _grid.Columns.Count == 0)
            {
                return;
            }

            var columns = _grid.Columns
                .Select((column, index) => new DataGridColumnLayout(
                    ResolveColumnKey(column) ?? $"Column{index}",
                    column.DisplayIndex >= 0 ? column.DisplayIndex : index,
                    Math.Max(column.ActualWidth, 0d)))
                .ToList();

            DataGridLayoutStore.Save(_gridKey, new DataGridLayout(columns));
        }

        private static string? ResolveColumnKey(DataGridColumn column)
        {
            var columnKey = GetColumnKey(column);
            if (!string.IsNullOrWhiteSpace(columnKey))
            {
                return columnKey;
            }

            return column.Header?.ToString();
        }

        private sealed record ColumnReference(DataGridColumn Column, int OriginalIndex, string? Key);

        private sealed record RankedColumn(DataGridColumn Column, int OriginalIndex, int DisplayIndex, double? Width);
    }
}

internal static class DataGridLayoutStore
{
    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static Dictionary<string, DataGridLayout>? _cache;

    public static DataGridLayout? Load(string gridKey)
    {
        lock (SyncRoot)
        {
            EnsureCacheLoaded();
            return _cache!.TryGetValue(gridKey, out var layout)
                ? layout
                : null;
        }
    }

    public static void Save(string gridKey, DataGridLayout layout)
    {
        lock (SyncRoot)
        {
            EnsureCacheLoaded();
            _cache![gridKey] = layout;

            var directory = GetSettingsDirectory();
            Directory.CreateDirectory(directory);
            File.WriteAllText(GetSettingsPath(), JsonSerializer.Serialize(_cache, SerializerOptions));
        }
    }

    private static void EnsureCacheLoaded()
    {
        if (_cache is not null)
        {
            return;
        }

        var settingsPath = GetSettingsPath();
        if (!File.Exists(settingsPath))
        {
            _cache = new Dictionary<string, DataGridLayout>(StringComparer.Ordinal);
            return;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            _cache = JsonSerializer.Deserialize<Dictionary<string, DataGridLayout>>(json, SerializerOptions)
                ?? new Dictionary<string, DataGridLayout>(StringComparer.Ordinal);
        }
        catch
        {
            _cache = new Dictionary<string, DataGridLayout>(StringComparer.Ordinal);
        }
    }

    private static string GetSettingsPath() => Path.Combine(GetSettingsDirectory(), "ui-grid-layouts.json");

    private static string GetSettingsDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share");
        }

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = Path.GetTempPath();
        }

        return Path.Combine(localAppData, "PayrollApp");
    }
}

internal sealed record DataGridLayout(List<DataGridColumnLayout> Columns);

internal sealed record DataGridColumnLayout(string ColumnKey, int DisplayIndex, double Width);
