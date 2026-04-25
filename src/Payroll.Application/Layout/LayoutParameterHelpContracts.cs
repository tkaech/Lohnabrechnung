namespace Payroll.Application.Layout;

public static class LayoutParameterHelpParameterTypes
{
    public const string Color = "color";
    public const string Number = "number";
    public const string Spacing = "spacing";
    public const string Size = "size";
    public const string Boolean = "boolean";
    public const string Text = "text";
    public const string Path = "path";
}

public static class LayoutParameterHelpPreviewKinds
{
    public const string PagePadding = "page-padding";
    public const string PanelPadding = "panel-padding";
    public const string SectionSpacing = "section-spacing";
    public const string TableCellPadding = "table-cell-padding";
    public const string CornerRadius = "corner-radius";
    public const string FontFamily = "font-family";
    public const string FontSize = "font-size";
    public const string TextColor = "text-color";
    public const string MutedTextColor = "muted-text-color";
    public const string BackgroundColor = "background-color";
    public const string AccentColor = "accent-color";
    public const string LogoText = "logo-text";
    public const string LogoPath = "logo-path";
}

public sealed record LayoutParameterHelpItemDto(
    string Name,
    string Description,
    string Source,
    string ParameterType,
    string ExampleValue,
    string PreviewKind,
    string PreviewTarget,
    string PreviewExplanation,
    decimal? Step = null);

public sealed record LayoutParameterHelpGroupDto(
    string Key,
    string Title,
    string Summary,
    IReadOnlyCollection<LayoutParameterHelpItemDto> Parameters);
