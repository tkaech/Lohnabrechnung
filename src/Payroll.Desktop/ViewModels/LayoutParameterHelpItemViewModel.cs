using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Payroll.Application.Layout;

namespace Payroll.Desktop.ViewModels;

public sealed class LayoutParameterHelpItemViewModel : ViewModelBase
{
    private static readonly SolidColorBrush DefaultSurfaceBrush = new(Color.Parse("#FFF5F7FA"));
    private static readonly SolidColorBrush DefaultTextBrush = new(Color.Parse("#FF1A2530"));
    private static readonly SolidColorBrush DefaultMutedTextBrush = new(Color.Parse("#FF5F6B7A"));
    private static readonly SolidColorBrush DefaultAccentBrush = new(Color.Parse("#FF14324A"));
    private string _localValue;
    private bool _localBooleanValue;

    public LayoutParameterHelpItemViewModel(LayoutParameterHelpItemDto item)
    {
        Name = item.Name;
        Description = item.Description;
        Source = item.Source;
        ParameterType = item.ParameterType;
        ExampleValue = item.ExampleValue;
        PreviewKind = item.PreviewKind;
        PreviewTarget = item.PreviewTarget;
        PreviewExplanation = item.PreviewExplanation;
        Step = item.Step ?? 1m;
        _localValue = item.ExampleValue;
        _localBooleanValue = string.Equals(item.ExampleValue, "true", StringComparison.OrdinalIgnoreCase);
        IncreaseNumericValueCommand = new DelegateCommand(IncreaseNumericValue, () => ShowNumericEditor);
        DecreaseNumericValueCommand = new DelegateCommand(DecreaseNumericValue, () => ShowNumericEditor);
        ColorPresets = CreateColorPresets();
    }

    public string Name { get; }

    public string Description { get; }

    public string Source { get; }

    public string ParameterType { get; }

    public string ExampleValue { get; }

    public string PreviewKind { get; }

    public string PreviewTarget { get; }

    public string PreviewExplanation { get; }

    public decimal Step { get; }

    public ObservableCollection<LayoutParameterColorPresetViewModel> ColorPresets { get; }

    public DelegateCommand IncreaseNumericValueCommand { get; }

    public DelegateCommand DecreaseNumericValueCommand { get; }

    public string LocalValue
    {
        get => _localValue;
        set
        {
            if (!SetProperty(ref _localValue, value))
            {
                return;
            }

            if (IsBooleanParameter && bool.TryParse(value, out var parsed))
            {
                _localBooleanValue = parsed;
                RaisePropertyChanged(nameof(LocalBooleanValue));
            }

            RaisePreviewChanged();
        }
    }

    public bool LocalBooleanValue
    {
        get => _localBooleanValue;
        set
        {
            if (!SetProperty(ref _localBooleanValue, value))
            {
                return;
            }

            _localValue = value ? "true" : "false";
            RaisePropertyChanged(nameof(LocalValue));
            RaisePreviewChanged();
        }
    }

    public string ParameterTypeLabel => ParameterType switch
    {
        LayoutParameterHelpParameterTypes.Color => "Color",
        LayoutParameterHelpParameterTypes.Spacing => "Spacing",
        LayoutParameterHelpParameterTypes.Size => "Size",
        LayoutParameterHelpParameterTypes.Number => "Number",
        LayoutParameterHelpParameterTypes.Boolean => "Boolean",
        LayoutParameterHelpParameterTypes.Path => "Path",
        _ => "Text"
    };

    public bool IsColorParameter => ParameterType == LayoutParameterHelpParameterTypes.Color;

    public bool IsBooleanParameter => ParameterType == LayoutParameterHelpParameterTypes.Boolean;

    public bool ShowNumericEditor => ParameterType is LayoutParameterHelpParameterTypes.Number
        or LayoutParameterHelpParameterTypes.Spacing
        or LayoutParameterHelpParameterTypes.Size;

    public bool ShowPlainValueEditor => !IsColorParameter && !IsBooleanParameter && !ShowNumericEditor;

    public string ValueInputLabel => IsColorParameter ? "Testwert (Hex)" : ShowNumericEditor ? "Testwert (Zahl)" : IsBooleanParameter ? "Testwert (Boolean)" : "Testwert";

    public IBrush ColorSwatchBrush => ResolveColorBrush(DefaultSurfaceBrush);

    public string ColorSwatchLabel => IsColorParameter ? ResolvedColorText : string.Empty;

    public string ResolvedColorText => TryParseColor(LocalValue, out var color)
        ? color.ToString().ToUpperInvariant()
        : ExampleValue;

    public bool ShowPagePaddingPreview => PreviewKind == LayoutParameterHelpPreviewKinds.PagePadding;

    public bool ShowPanelPaddingPreview => PreviewKind == LayoutParameterHelpPreviewKinds.PanelPadding;

    public bool ShowSectionSpacingPreview => PreviewKind == LayoutParameterHelpPreviewKinds.SectionSpacing;

    public bool ShowTableCellPaddingPreview => PreviewKind == LayoutParameterHelpPreviewKinds.TableCellPadding;

    public bool ShowCornerRadiusPreview => PreviewKind == LayoutParameterHelpPreviewKinds.CornerRadius;

    public bool ShowFontFamilyPreview => PreviewKind == LayoutParameterHelpPreviewKinds.FontFamily;

    public bool ShowFontSizePreview => PreviewKind == LayoutParameterHelpPreviewKinds.FontSize;

    public bool ShowTextColorPreview => PreviewKind == LayoutParameterHelpPreviewKinds.TextColor;

    public bool ShowMutedTextColorPreview => PreviewKind == LayoutParameterHelpPreviewKinds.MutedTextColor;

    public bool ShowBackgroundColorPreview => PreviewKind == LayoutParameterHelpPreviewKinds.BackgroundColor;

    public bool ShowAccentColorPreview => PreviewKind == LayoutParameterHelpPreviewKinds.AccentColor;

    public bool ShowLogoTextPreview => PreviewKind == LayoutParameterHelpPreviewKinds.LogoText;

    public bool ShowLogoPathPreview => PreviewKind == LayoutParameterHelpPreviewKinds.LogoPath;

    public Thickness PagePaddingPreviewMargin => new(ClampNumeric(LocalValue, ExampleValue, 0d, 32d));

    public Thickness PanelPaddingPreviewPadding => new(ClampNumeric(LocalValue, ExampleValue, 0d, 24d));

    public double SectionSpacingPreviewSpacing => ClampNumeric(LocalValue, ExampleValue, 0d, 24d);

    public Thickness TableCellPaddingPreviewPadding => new(8d, ClampNumeric(LocalValue, ExampleValue, 0d, 16d));

    public CornerRadius CornerRadiusPreviewValue => new(ClampNumeric(LocalValue, ExampleValue, 0d, 24d));

    public string FontFamilyPreviewValue => string.IsNullOrWhiteSpace(LocalValue) ? ExampleValue : LocalValue.Trim();

    public double FontSizePreviewValue => ClampNumeric(LocalValue, ExampleValue, 8d, 28d);

    public IBrush TextColorPreviewBrush => ResolveColorBrush(DefaultTextBrush);

    public IBrush MutedTextColorPreviewBrush => ResolveColorBrush(DefaultMutedTextBrush);

    public IBrush BackgroundColorPreviewBrush => ResolveColorBrush(DefaultSurfaceBrush);

    public IBrush AccentColorPreviewBrush => ResolveColorBrush(DefaultAccentBrush);

    public string LogoTextPreviewValue => string.IsNullOrWhiteSpace(LocalValue) ? ExampleValue : LocalValue.Trim();

    public string LogoPathPreviewValue => string.IsNullOrWhiteSpace(LocalValue) ? ExampleValue : LocalValue.Trim();

    public void ApplyCurrentValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        LocalValue = value.Trim();
    }

    private ObservableCollection<LayoutParameterColorPresetViewModel> CreateColorPresets()
    {
        if (!IsColorParameter)
        {
            return [];
        }

        var values = new[]
        {
            ExampleValue,
            "#FF1A2530",
            "#FF5F6B7A",
            "#FFF5F7FA",
            "#FF14324A",
            "#FF336699",
            "#FFB54708"
        }
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

        return new ObservableCollection<LayoutParameterColorPresetViewModel>(
            values.Select(value => new LayoutParameterColorPresetViewModel
            {
                HexValue = value.ToUpperInvariant(),
                Brush = TryParseColor(value, out var color) ? new SolidColorBrush(color) : DefaultSurfaceBrush,
                SelectCommand = new DelegateCommand(() => LocalValue = value.ToUpperInvariant())
            }));
    }

    private void IncreaseNumericValue()
    {
        UpdateNumericValue(Step);
    }

    private void DecreaseNumericValue()
    {
        UpdateNumericValue(-Step);
    }

    private void UpdateNumericValue(decimal delta)
    {
        var current = TryParseDecimal(LocalValue, out var parsed)
            ? parsed
            : TryParseDecimal(ExampleValue, out parsed)
                ? parsed
                : 0m;
        var updated = current + delta;
        LocalValue = updated % 1m == 0m
            ? decimal.Truncate(updated).ToString(CultureInfo.InvariantCulture)
            : updated.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void RaisePreviewChanged()
    {
        RaisePropertyChanged(nameof(ColorSwatchBrush));
        RaisePropertyChanged(nameof(ColorSwatchLabel));
        RaisePropertyChanged(nameof(ResolvedColorText));
        RaisePropertyChanged(nameof(PagePaddingPreviewMargin));
        RaisePropertyChanged(nameof(PanelPaddingPreviewPadding));
        RaisePropertyChanged(nameof(SectionSpacingPreviewSpacing));
        RaisePropertyChanged(nameof(TableCellPaddingPreviewPadding));
        RaisePropertyChanged(nameof(CornerRadiusPreviewValue));
        RaisePropertyChanged(nameof(FontFamilyPreviewValue));
        RaisePropertyChanged(nameof(FontSizePreviewValue));
        RaisePropertyChanged(nameof(TextColorPreviewBrush));
        RaisePropertyChanged(nameof(MutedTextColorPreviewBrush));
        RaisePropertyChanged(nameof(BackgroundColorPreviewBrush));
        RaisePropertyChanged(nameof(AccentColorPreviewBrush));
        RaisePropertyChanged(nameof(LogoTextPreviewValue));
        RaisePropertyChanged(nameof(LogoPathPreviewValue));
        IncreaseNumericValueCommand.RaiseCanExecuteChanged();
        DecreaseNumericValueCommand.RaiseCanExecuteChanged();
    }

    private IBrush ResolveColorBrush(IBrush fallback)
    {
        return TryParseColor(LocalValue, out var color)
            ? new SolidColorBrush(color)
            : fallback;
    }

    private static bool TryParseColor(string? value, out Color color)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            try
            {
                color = Color.Parse(value.Trim());
                return true;
            }
            catch
            {
            }
        }

        color = default;
        return false;
    }

    private static double ClampNumeric(string value, string fallback, double min, double max)
    {
        var resolved = TryParseDecimal(value, out var parsed)
            ? (double)parsed
            : TryParseDecimal(fallback, out parsed)
                ? (double)parsed
                : min;

        return Math.Clamp(resolved, min, max);
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0m;
            return false;
        }

        var normalized = value.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
