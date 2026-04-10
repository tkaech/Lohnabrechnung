namespace Payroll.Domain.Settings;

public sealed record PayrollPreviewHelpVisibility(
    string Code,
    bool IsEnabled,
    string HelpText);
