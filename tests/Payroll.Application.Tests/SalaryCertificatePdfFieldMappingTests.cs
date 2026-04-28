using System.Reflection;
using Payroll.Application.AnnualSalary;
using Payroll.Application.SalaryCertificate;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.SalaryCertificate;

namespace Payroll.Application.Tests;

public sealed class SalaryCertificatePdfFieldMappingTests
{
    [Fact]
    public void CreateInitialMapping_ContainsAllSalaryCertificateFieldCodes()
    {
        var expectedCodes = typeof(SalaryCertificateFieldCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        var mappings = SalaryCertificatePdfFieldMappingCatalog.CreateInitialMapping();
        var mappedCodes = mappings
            .Select(mapping => mapping.SalaryCertificateFieldCode)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(expectedCodes, code => Assert.Contains(code, mappedCodes));
    }

    [Fact]
    public void CreateInitialMapping_MapsAllRequiredFieldsToPdfFieldNames()
    {
        var mappings = SalaryCertificatePdfFieldMappingCatalog.CreateInitialMapping();

        Assert.All(
            mappings.Where(mapping => mapping.IsRequired),
            mapping => Assert.False(string.IsNullOrWhiteSpace(mapping.PdfFieldName)));
    }

    [Fact]
    public async Task CreateInitialMapping_ValidatesAgainstProjectTemplate()
    {
        var templatePath = GetWorkspaceTemplatePath();
        var fieldNames = await new PdfFormFieldReader().ReadFieldNamesAsync(templatePath);

        var result = SalaryCertificatePdfFieldMappingCatalog.ValidateRequiredFields(
            SalaryCertificatePdfFieldMappingCatalog.CreateInitialMapping(),
            fieldNames);

        Assert.False(result.HasMissingRequiredFields);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidateRequiredFields_ReportsMissingRequiredPdfFields()
    {
        var mappings = new[]
        {
            new SalaryCertificatePdfFieldMappingDto(
                SalaryCertificateFieldCodes.CertificateYear,
                "YearField",
                SalaryCertificatePdfFieldFormat.Text,
                true),
            new SalaryCertificatePdfFieldMappingDto(
                SalaryCertificateFieldCodes.EmployeeAhvNumber,
                "MissingAhvField",
                SalaryCertificatePdfFieldFormat.Text,
                true),
            new SalaryCertificatePdfFieldMappingDto(
                SalaryCertificateFieldCodes.ExpensesCode13,
                "MissingOptionalExpensesField",
                SalaryCertificatePdfFieldFormat.ChfAmount,
                false)
        };

        var result = SalaryCertificatePdfFieldMappingCatalog.ValidateRequiredFields(
            mappings,
            ["YearField"]);

        Assert.True(result.HasMissingRequiredFields);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(SalaryCertificateFieldCodes.EmployeeAhvNumber, issue.SalaryCertificateFieldCode);
        Assert.Equal("MissingAhvField", issue.PdfFieldName);
    }

    [Fact]
    public void SalaryCertificateService_HasNoPdfFieldMappingDependency()
    {
        var constructorParameters = typeof(SalaryCertificateService)
            .GetConstructors()
            .Single()
            .GetParameters();
        var privateFields = typeof(SalaryCertificateService)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(AnnualSalaryService));
        Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType.Name.Contains("Pdf", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(privateFields, field => field.FieldType.Name.Contains("Pdf", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PdfFormFieldReader_ReadFieldNamesAsync_ExtractsLiteralAndHexFieldNames()
    {
        var templatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        await File.WriteAllTextAsync(
            templatePath,
            "%PDF-1.4\n1 0 obj << /T (employee.ahv\\)number) >> endobj\n2 0 obj << /T <596561724669656C64> >> endobj\n");

        try
        {
            var fieldNames = await new PdfFormFieldReader().ReadFieldNamesAsync(templatePath);

            Assert.Contains("employee.ahv)number", fieldNames);
            Assert.Contains("YearField", fieldNames);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    [Fact]
    public async Task PdfFormFieldReader_ReadFieldNamesAsync_ReadsProjectTemplate()
    {
        var templatePath = GetWorkspaceTemplatePath();

        var fieldNames = await new PdfFormFieldReader().ReadFieldNamesAsync(templatePath);

        Assert.True(fieldNames.Count >= 40);
        Assert.Contains("AHVLinks_C", fieldNames);
        Assert.Contains("TextLinks_C-GebDatum", fieldNames);
        Assert.Contains("TextLinks_D", fieldNames);
        Assert.Contains("DezZahlNull_1", fieldNames);
        Assert.Contains("DezZahlNull_8", fieldNames);
        Assert.Contains("DezZahlNull_9", fieldNames);
        Assert.Contains("DezZahlNull_10_1", fieldNames);
        Assert.Contains("DezZahlNull_11", fieldNames);
        Assert.Contains("DezZahlNull_12", fieldNames);
        Assert.Contains("DezZahlNull_13_1_2", fieldNames);
    }

    private static string GetWorkspaceTemplatePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var designSystemPath = Path.Combine(current.FullName, "src", "Payroll.Desktop", "Styles", "DesignSystem.axaml");
            if (File.Exists(designSystemPath))
            {
                var candidate = Path.Combine(current.FullName, PayrollSettings.DefaultSalaryCertificatePdfTemplatePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Lohnausweis-Vorlage im Workspace wurde nicht gefunden.");
    }
}
