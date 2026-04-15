using System.Reflection;

namespace Payroll.Application.Reporting;

public static class PayrollStatementTemplateProvider
{
    private const string ResourceName = "Payroll.Application.Reporting.Templates.PayrollStatementTemplate.txt";

    public static string LoadDefaultTemplate()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Payroll-Template '{ResourceName}' konnte nicht geladen werden.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
