using Payroll.Desktop.ViewModels;

namespace Payroll.Application.Tests;

public sealed class LayoutParameterHelpViewModelTests
{
    [Fact]
    public void DefaultsToOverview_AndLoadsParameterGroups()
    {
        var viewModel = new LayoutParameterHelpViewModel();

        Assert.True(viewModel.ShowOverviewSection);
        Assert.False(viewModel.ShowLayoutParametersSection);
        Assert.Equal(2, viewModel.NavigationItems.Count);
        Assert.Equal(4, viewModel.Groups.Count);
    }

    [Fact]
    public void SelectingLayoutParameters_ShowsDedicatedSubpage()
    {
        var viewModel = new LayoutParameterHelpViewModel();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems.Single(item => item.Section == HelpSection.LayoutParameters);

        Assert.False(viewModel.ShowOverviewSection);
        Assert.True(viewModel.ShowLayoutParametersSection);
        Assert.Contains(viewModel.Groups, group => group.Parameters.Any(parameter => parameter.Name == "Theme.Layout.PagePadding"));
        Assert.Contains(viewModel.Groups, group => group.Parameters.Any(parameter => parameter.Name == "AppAccentColorHex"));
    }

    [Fact]
    public void ParameterRow_UsesLocalPreviewValueWithoutGlobalMutation()
    {
        var viewModel = new LayoutParameterHelpViewModel();
        var pagePadding = viewModel.Groups
            .SelectMany(group => group.Parameters)
            .Single(parameter => parameter.Name == "Theme.Layout.PagePadding");
        var accentColor = viewModel.Groups
            .SelectMany(group => group.Parameters)
            .Single(parameter => parameter.Name == "AppAccentColorHex");

        pagePadding.LocalValue = "28";
        accentColor.LocalValue = "#FF336699";

        Assert.Equal(28d, pagePadding.PagePaddingPreviewMargin.Left);
        Assert.True(accentColor.IsColorParameter);
        Assert.Equal("#FF336699", accentColor.ResolvedColorText);
        Assert.True(pagePadding.ShowNumericEditor);
    }

    [Fact]
    public void ApplyCurrentValues_OverridesLocalDefaults()
    {
        var viewModel = new LayoutParameterHelpViewModel();
        viewModel.ApplyCurrentValues(new Dictionary<string, string>
        {
            ["Theme.Layout.PagePadding"] = "24",
            ["AppFontFamily"] = "Segoe UI",
            ["AppAccentColorHex"] = "#FF224466"
        });

        var pagePadding = viewModel.Groups.SelectMany(group => group.Parameters).Single(parameter => parameter.Name == "Theme.Layout.PagePadding");
        var fontFamily = viewModel.Groups.SelectMany(group => group.Parameters).Single(parameter => parameter.Name == "AppFontFamily");
        var accentColor = viewModel.Groups.SelectMany(group => group.Parameters).Single(parameter => parameter.Name == "AppAccentColorHex");

        Assert.Equal("24", pagePadding.LocalValue);
        Assert.Equal("Segoe UI", fontFamily.LocalValue);
        Assert.Equal("#FF224466", accentColor.ResolvedColorText);
    }

    [Fact]
    public void NumericAndColorHelpers_WorkLocallyPerRow()
    {
        var viewModel = new LayoutParameterHelpViewModel();
        var pagePadding = viewModel.Groups.SelectMany(group => group.Parameters).Single(parameter => parameter.Name == "Theme.Layout.PagePadding");
        var accentColor = viewModel.Groups.SelectMany(group => group.Parameters).Single(parameter => parameter.Name == "AppAccentColorHex");

        pagePadding.IncreaseNumericValueCommand.Execute(null);
        accentColor.ColorPresets.First(item => item.HexValue == "#FF336699").SelectCommand.Execute(null);

        Assert.Equal("22", pagePadding.LocalValue);
        Assert.Equal("#FF336699", accentColor.LocalValue);
    }

    [Fact]
    public void TableCellPaddingPreview_UsesLocalVerticalPadding()
    {
        var viewModel = new LayoutParameterHelpViewModel();
        var tablePadding = viewModel.Groups
            .SelectMany(group => group.Parameters)
            .Single(parameter => parameter.Name == "Theme.Layout.TableCellVerticalPadding");

        tablePadding.LocalValue = "4";

        Assert.True(tablePadding.ShowTableCellPaddingPreview);
        Assert.Equal(4d, tablePadding.TableCellPaddingPreviewPadding.Top);
        Assert.Equal(8d, tablePadding.TableCellPaddingPreviewPadding.Left);
    }
}
