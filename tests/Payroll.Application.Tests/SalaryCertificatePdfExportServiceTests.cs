using System.Text;
using Payroll.Application.AnnualSalary;
using Payroll.Application.SalaryCertificate;
using Payroll.Application.Settings;
using Payroll.Domain.Employees;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.SalaryCertificate;

namespace Payroll.Application.Tests;

public sealed class SalaryCertificatePdfExportServiceTests
{
    [Fact]
    public async Task ExportAsync_WritesPdfFromTemplateAndKeepsTemplateUnchanged()
    {
        var templatePath = GetWorkspaceTemplatePath();
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        var originalTemplateBytes = await File.ReadAllBytesAsync(templatePath);
        var recordRepository = new CaptureSalaryCertificateRecordRepository();

        try
        {
            var service = CreateExportService(
                templatePath,
                new PdfFormFieldReader(),
                new SalaryCertificatePdfDocumentWriter(),
                recordRepository);

            var exportPath = await service.ExportAsync(
                new SalaryCertificatePdfExportCommand(EmployeeId, 2026, outputPath));

            Assert.Equal(outputPath, exportPath);
            Assert.True(File.Exists(outputPath));
            Assert.Equal(originalTemplateBytes, await File.ReadAllBytesAsync(templatePath));

            var exportedContent = Encoding.Latin1.GetString(await File.ReadAllBytesAsync(outputPath));
            Assert.Contains("/NeedAppearances true", exportedContent, StringComparison.Ordinal);
            Assert.Contains("/V (2026)", exportedContent, StringComparison.Ordinal);
            Assert.Contains("/V (756.1234.5678.97)", exportedContent, StringComparison.Ordinal);
            Assert.Contains("/V (01.01.1990)", exportedContent, StringComparison.Ordinal);
            Assert.Contains("/V (1000.00)", exportedContent, StringComparison.Ordinal);
            var record = Assert.Single(recordRepository.Records);
            Assert.Equal(EmployeeId, record.EmployeeId);
            Assert.Equal(2026, record.Year);
            Assert.Equal(outputPath, record.OutputFilePath);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenRequiredPdfFieldIsMissing()
    {
        var templatePath = GetWorkspaceTemplatePath();
        var fieldNames = SalaryCertificatePdfFieldMappingCatalog.CreateInitialMapping()
            .Select(mapping => mapping.PdfFieldName)
            .Where(name => !string.Equals(name, "AHVLinks_C", StringComparison.Ordinal))
            .ToArray();

        var service = CreateExportService(
            templatePath,
            new StubPdfFormFieldReader(fieldNames),
            new CapturePdfDocumentWriter());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(
            new SalaryCertificatePdfExportCommand(EmployeeId, 2026, Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf"))));

        Assert.Contains(SalaryCertificateFieldCodes.EmployeeAhvNumber, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenTemplateIsMissing()
    {
        var templatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.missing.pdf");
        var service = CreateExportService(
            templatePath,
            new StubPdfFormFieldReader([]),
            new CapturePdfDocumentWriter());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(
            new SalaryCertificatePdfExportCommand(EmployeeId, 2026, Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf"))));

        Assert.Contains("Lohnausweis-Vorlage nicht gefunden", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportAsync_FormatsAndPassesAllMappedFieldsToWriter()
    {
        var templatePath = GetWorkspaceTemplatePath();
        var fieldNames = SalaryCertificatePdfFieldMappingCatalog.CreateInitialMapping()
            .Select(mapping => mapping.PdfFieldName)
            .ToArray();
        var writer = new CapturePdfDocumentWriter();
        var service = CreateExportService(
            templatePath,
            new StubPdfFormFieldReader(fieldNames),
            writer);

        await service.ExportAsync(
            new SalaryCertificatePdfExportCommand(EmployeeId, 2026, Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf")));

        Assert.NotNull(writer.LastFields);
        Assert.Contains(writer.LastFields!, field => field.PdfFieldName == "TextLinks_D" && field.Value == "2026");
        Assert.Contains(writer.LastFields!, field => field.PdfFieldName == "TextLinks_C-GebDatum" && field.Value == "01.01.1990");
        Assert.Contains(writer.LastFields!, field => field.PdfFieldName == "DezZahlNull_11" && field.Value == "825.00");
        Assert.Equal(templatePath, writer.LastTemplatePath);
    }

    [Fact]
    public async Task ExportAsync_DoesNotStoreRecordWhenWriterFails()
    {
        var templatePath = GetWorkspaceTemplatePath();
        var recordRepository = new CaptureSalaryCertificateRecordRepository();
        var service = CreateExportService(
            templatePath,
            new StubPdfFormFieldReader(SalaryCertificatePdfFieldMappingCatalog.CreateInitialMapping().Select(mapping => mapping.PdfFieldName).ToArray()),
            new ThrowingPdfDocumentWriter("kaputt"),
            recordRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(
            new SalaryCertificatePdfExportCommand(EmployeeId, 2026, Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf"))));

        Assert.Empty(recordRepository.Records);
        Assert.Equal(0, recordRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetLatestRecordAsync_ReturnsLatestRecordForEmployeeAndYear()
    {
        var repository = new CaptureSalaryCertificateRecordRepository();
        var service = CreateExportService(
            GetWorkspaceTemplatePath(),
            new StubPdfFormFieldReader(SalaryCertificatePdfFieldMappingCatalog.CreateInitialMapping().Select(mapping => mapping.PdfFieldName).ToArray()),
            new CapturePdfDocumentWriter(),
            repository);
        repository.Add(new global::Payroll.Domain.SalaryCertificate.SalaryCertificateRecord(EmployeeId, 2026, "/tmp/first.pdf"));
        await Task.Delay(5);
        repository.Add(new global::Payroll.Domain.SalaryCertificate.SalaryCertificateRecord(EmployeeId, 2026, "/tmp/second.pdf"));

        var latest = await service.GetLatestRecordAsync(new SalaryCertificateRecordQuery(EmployeeId, 2026));

        Assert.NotNull(latest);
        Assert.Equal("/tmp/second.pdf", latest!.OutputFilePath);
    }

    private static SalaryCertificatePdfExportService CreateExportService(
        string templatePath,
        ISalaryCertificatePdfFormFieldReader fieldReader,
        ISalaryCertificatePdfDocumentWriter documentWriter,
        ISalaryCertificateRecordRepository? recordRepository = null)
    {
        var annualSalaryService = new AnnualSalaryService(new StubAnnualSalaryRepository());
        var salaryCertificateService = new SalaryCertificateService(annualSalaryService);
        var settingsService = new PayrollSettingsService(new StubPayrollSettingsRepository(templatePath));

        return new SalaryCertificatePdfExportService(
            salaryCertificateService,
            settingsService,
            fieldReader,
            documentWriter,
            recordRepository ?? new CaptureSalaryCertificateRecordRepository());
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

    private static readonly Guid EmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed class StubAnnualSalaryRepository : IAnnualSalaryRepository
    {
        public Task<AnnualSalaryOverviewDto> GetOverviewAsync(AnnualSalaryOverviewQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AnnualSalaryOverviewDto(
                query.EmployeeId,
                "9000",
                "Yvonne",
                "Kaech",
                "756.1234.5678.97",
                new DateOnly(1990, 1, 1),
                query.Year,
                [],
                new AnnualSalaryTotalsDto(
                    1000m,
                    80m,
                    20m,
                    0m,
                    0m,
                    100m,
                    50m,
                    25m,
                    30m,
                    825m)));
        }
    }

    private sealed class StubPayrollSettingsRepository : IPayrollSettingsRepository
    {
        private readonly string _templatePath;

        public StubPayrollSettingsRepository(string templatePath)
        {
            _templatePath = templatePath;
        }

        public Task<PayrollSettingsDto> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new PayrollSettingsDto(
                string.Empty,
                PayrollSettings.DefaultAppFontFamily,
                PayrollSettings.DefaultAppFontSize,
                PayrollSettings.DefaultAppTextColorHex,
                PayrollSettings.DefaultAppMutedTextColorHex,
                PayrollSettings.DefaultAppBackgroundColorHex,
                PayrollSettings.DefaultAppAccentColorHex,
                PayrollSettings.DefaultAppLogoText,
                string.Empty,
                PayrollSettings.DefaultPrintFontFamily,
                PayrollSettings.DefaultPrintFontSize,
                PayrollSettings.DefaultPrintTextColorHex,
                PayrollSettings.DefaultPrintMutedTextColorHex,
                PayrollSettings.DefaultPrintAccentColorHex,
                PayrollSettings.DefaultPrintLogoText,
                string.Empty,
                string.Empty,
                _templatePath,
                PayrollSettings.DefaultDecimalSeparator,
                PayrollSettings.DefaultThousandsSeparator,
                PayrollSettings.DefaultCurrencyCode,
                null,
                null,
                null,
                PayrollSettings.DefaultAhvIvEoRate,
                PayrollSettings.DefaultAlvRate,
                PayrollSettings.DefaultSicknessAccidentInsuranceRate,
                PayrollSettings.DefaultTrainingAndHolidayRate,
                PayrollSettings.DefaultVacationCompensationRate,
                PayrollSettings.DefaultVacationCompensationRateAge50Plus,
                0m,
                0m,
                0m,
                [],
                [],
                [],
                []));
        }

        public Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(WorkTimeSupplementSettings.Empty);
        }

        public Task<PayrollSettingsDto> SaveAsync(SavePayrollSettingsCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubPdfFormFieldReader : ISalaryCertificatePdfFormFieldReader
    {
        private readonly IReadOnlyCollection<string> _fieldNames;

        public StubPdfFormFieldReader(IReadOnlyCollection<string> fieldNames)
        {
            _fieldNames = fieldNames;
        }

        public Task<IReadOnlyCollection<string>> ReadFieldNamesAsync(string templatePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_fieldNames);
        }
    }

    private sealed class ThrowingPdfDocumentWriter : ISalaryCertificatePdfDocumentWriter
    {
        private readonly string _message;

        public ThrowingPdfDocumentWriter(string message)
        {
            _message = message;
        }

        public Task WriteAsync(string templatePath, string outputPath, IReadOnlyCollection<SalaryCertificatePdfFieldWriteDto> fields, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(_message);
        }
    }

    private sealed class CaptureSalaryCertificateRecordRepository : ISalaryCertificateRecordRepository
    {
        public List<global::Payroll.Domain.SalaryCertificate.SalaryCertificateRecord> Records { get; } = [];
        public int SaveChangesCallCount { get; private set; }

        public void Add(global::Payroll.Domain.SalaryCertificate.SalaryCertificateRecord record)
        {
            Records.Add(record);
        }

        public Task<SalaryCertificateRecordDto?> GetLatestAsync(Guid employeeId, int year, CancellationToken cancellationToken = default)
        {
            var latest = Records
                .Where(record => record.EmployeeId == employeeId && record.Year == year)
                .OrderByDescending(record => record.CreatedAtUtc)
                .Select(record => new SalaryCertificateRecordDto(
                    record.Id,
                    record.EmployeeId,
                    record.Year,
                    record.CreatedAtUtc,
                    record.OutputFilePath,
                    record.FileHash))
                .FirstOrDefault();

            return Task.FromResult(latest);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class CapturePdfDocumentWriter : ISalaryCertificatePdfDocumentWriter
    {
        public string? LastTemplatePath { get; private set; }

        public string? LastOutputPath { get; private set; }

        public IReadOnlyCollection<SalaryCertificatePdfFieldWriteDto>? LastFields { get; private set; }

        public Task WriteAsync(
            string templatePath,
            string outputPath,
            IReadOnlyCollection<SalaryCertificatePdfFieldWriteDto> fields,
            CancellationToken cancellationToken = default)
        {
            LastTemplatePath = templatePath;
            LastOutputPath = outputPath;
            LastFields = fields;
            return Task.CompletedTask;
        }
    }
}
