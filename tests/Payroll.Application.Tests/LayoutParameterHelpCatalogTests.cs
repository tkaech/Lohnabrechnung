using Payroll.Application.Layout;

namespace Payroll.Application.Tests;

public sealed class LayoutParameterHelpCatalogTests
{
    [Fact]
    public void GetGroups_ReturnsStructuredLayoutHelpMetadata()
    {
        var groups = LayoutParameterHelpCatalog.GetGroups();
        var accentColor = groups
            .Single(group => group.Key == "colors-branding")
            .Parameters
            .Single(parameter => parameter.Name == "AppAccentColorHex");

        Assert.Equal(4, groups.Count);
        Assert.Contains(groups, group => group.Key == "spacing" && group.Parameters.Any(parameter => parameter.Name == "Theme.Layout.PagePadding"));
        Assert.Contains(groups, group => group.Key == "spacing" && group.Parameters.Any(parameter => parameter.Name == "Theme.Layout.TableCellVerticalPadding"));
        Assert.Contains(groups, group => group.Key == "surface-shape" && group.Parameters.Any(parameter => parameter.Name == "Theme.Layout.PanelCornerRadius"));
        Assert.Contains(groups, group => group.Key == "typography" && group.Parameters.Any(parameter => parameter.Name == "AppFontFamily"));
        Assert.Contains(groups, group => group.Key == "colors-branding" && group.Parameters.Any(parameter => parameter.Name == "AppAccentColorHex"));
        Assert.Equal(LayoutParameterHelpParameterTypes.Color, accentColor.ParameterType);
        Assert.Equal(LayoutParameterHelpPreviewKinds.AccentColor, accentColor.PreviewKind);
        Assert.Equal("#FF14324A", accentColor.ExampleValue);
        Assert.Equal("Akzente und Hervorhebungen", accentColor.PreviewTarget);
        Assert.False(string.IsNullOrWhiteSpace(accentColor.PreviewExplanation));
    }
}
