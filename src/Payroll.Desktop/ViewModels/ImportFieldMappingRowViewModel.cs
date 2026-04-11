using System.Collections.ObjectModel;

namespace Payroll.Desktop.ViewModels;

public sealed class ImportFieldMappingRowViewModel : ViewModelBase
{
    private string _fieldKey = string.Empty;
    private string _fieldLabel = string.Empty;
    private bool _isRequired;
    private bool _allowEmpty;
    private string _csvColumnSearchText = string.Empty;
    private string? _selectedCsvColumn;

    public ImportFieldMappingRowViewModel()
    {
        AvailableCsvColumns = [];
    }

    public string FieldKey
    {
        get => _fieldKey;
        set => SetProperty(ref _fieldKey, value);
    }

    public string FieldLabel
    {
        get => _fieldLabel;
        set => SetProperty(ref _fieldLabel, value);
    }

    public bool IsRequired
    {
        get => _isRequired;
        set => SetProperty(ref _isRequired, value);
    }

    public bool AllowEmpty
    {
        get => _allowEmpty;
        set => SetProperty(ref _allowEmpty, value);
    }

    public string? SelectedCsvColumn
    {
        get => _selectedCsvColumn;
        set => SetProperty(ref _selectedCsvColumn, value);
    }

    public ObservableCollection<string> AvailableCsvColumns { get; }

    public string CsvColumnSearchText
    {
        get => _csvColumnSearchText;
        set => SetProperty(ref _csvColumnSearchText, value);
    }

    public void ApplyAvailableCsvColumns(IEnumerable<string> headers)
    {
        var previousSelection = SelectedCsvColumn;
        AvailableCsvColumns.Clear();
        AvailableCsvColumns.Add(string.Empty);

        foreach (var header in headers.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            AvailableCsvColumns.Add(header);
        }

        if (!string.IsNullOrWhiteSpace(previousSelection)
            && !AvailableCsvColumns.Contains(previousSelection))
        {
            AvailableCsvColumns.Add(previousSelection);
        }

        SelectedCsvColumn = previousSelection;
        SetSearchTextFromSelection(previousSelection);
    }

    public void SetSearchTextFromSelection(string? value)
    {
        CsvColumnSearchText = value ?? string.Empty;
    }
}
