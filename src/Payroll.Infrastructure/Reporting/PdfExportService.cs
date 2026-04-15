using System.Globalization;
using System.Text;
using Payroll.Application.Reporting;

namespace Payroll.Infrastructure.Reporting;

public sealed class PdfExportService : IPdfExportService
{
    private const string AppDataDirectoryName = "PayrollApp";
    private const string ExportDirectoryName = "Lohnabrechnungen";
    private const decimal PageLeft = 40m;
    private const decimal PageRight = 555m;
    private const decimal TableLabelX = 44m;
    private const decimal TableQuantityX = 365m;
    private const decimal TableRateX = 432m;
    private const decimal TableAmountRightX = 548m;

    public async Task<string> ExportPayrollStatementAsync(PayrollStatementPdfDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var exportDirectory = GetExportDirectory();
        Directory.CreateDirectory(exportDirectory);

        var exportPath = Path.Combine(exportDirectory, SanitizeFileName(document.FileNameWithoutExtension) + ".pdf");
        var pdfBytes = BuildPdf(document);
        await File.WriteAllBytesAsync(exportPath, pdfBytes, cancellationToken);
        return exportPath;
    }

    private static byte[] BuildPdf(PayrollStatementPdfDocument document)
    {
        var streamBuilder = new StringBuilder();
        var style = PdfStyleSettings.FromDocument(document);
        var writer = new PdfPageWriter(streamBuilder, style);
        var template = PayrollStatementTemplate.Parse(document.TemplateContent, document.TemplatePlaceholders);

        var y = 805m;
        if (!string.IsNullOrWhiteSpace(template.Logo))
        {
            writer.WriteText(PageLeft, 820m, template.Logo, style.TitleFontSize + 2m, true, style.TextColor);
        }

        WriteAddressBlock(writer, 40m, ref y, template.CompanyBlock, true);
        WriteTextBlock(writer, 320m, 720m, template.EmployeeBlock);

        writer.WriteText(PageLeft, 645m, template.AhvLine, style.BodyFontSize, false);
        writer.FillRectangle(PageLeft, 610m, PageRight - PageLeft, 18m, style.AccentColor);
        writer.WriteText(46m, 615m, template.BannerTitle, style.TitleFontSize, true);
        writer.WriteText(150m, 615m, template.BannerMonth, style.TitleFontSize, true);

        WriteMetaRow(writer, 592m, template.MetaRows.ElementAtOrDefault(0));
        WriteMetaRow(writer, 571m, template.MetaRows.ElementAtOrDefault(1));
        WriteMetaRow(writer, 550m, template.MetaRows.ElementAtOrDefault(2));

        writer.WriteText(TableLabelX, 503m, template.TableHeader.Label, style.BodyFontSize, true);
        writer.WriteText(TableQuantityX, 503m, template.TableHeader.Quantity, style.BodyFontSize, true);
        writer.WriteText(TableRateX, 503m, template.TableHeader.Rate, style.BodyFontSize, true);
        writer.WriteTextRight(TableAmountRightX, 503m, template.TableHeader.Amount, style.BodyFontSize, true);
        writer.DrawLine(PageLeft, 496m, PageRight, 496m, 0.5m);

        y = 478m;
        foreach (var bodyItem in template.BodyItems)
        {
            switch (bodyItem)
            {
                case PayrollStatementTemplateLineItem lineItem:
                    y = WritePayrollLine(writer, y, lineItem.Line);
                    break;
                case PayrollStatementTemplateSpacerItem spacerItem:
                    y -= spacerItem.Height;
                    break;
            }
        }

        y -= 4m;
        y -= 24m;

        foreach (var note in template.Notes)
        {
            var wrapped = WrapTextToWidth(note, 470m, 8.5m).ToArray();
            foreach (var wrappedLine in wrapped)
            {
                writer.WriteText(44m, y, wrappedLine, style.CaptionFontSize, false, style.MutedTextColor);
                y -= 12m;
            }

            y -= 3m;
        }

        y -= 22m;
        foreach (var closingLine in template.ClosingLines)
        {
            writer.WriteText(40m, y, closingLine.Text, closingLine.IsBold ? style.BodyFontSize : style.BodyFontSize, closingLine.IsBold);
            y -= closingLine.IsBold ? 28m : 18m;
        }

        return PdfDocumentBuilder.Build(streamBuilder.ToString(), style);
    }

    private static decimal WritePayrollLine(PdfPageWriter writer, decimal y, PayrollStatementPdfLineDto line)
    {
        var labelFontSize = line.IsEmphasized ? writer.Style.TitleFontSize : writer.Style.BodyFontSize;
        var labelLines = WrapTextToWidth(line.Label, 300m, labelFontSize).ToList();
        if (labelLines.Count == 0)
        {
            labelLines.Add(string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(line.Detail))
        {
            labelLines.AddRange(WrapTextToWidth(line.Detail, 300m, writer.Style.CaptionFontSize));
        }

        var rowHeight = 4m;
        for (var index = 0; index < labelLines.Count; index++)
        {
            rowHeight += index == 0 ? 10m : 8m;
        }

        rowHeight = Math.Max(14m, rowHeight);
        if (line.IsEmphasized)
        {
            writer.FillRectangle(PageLeft, y - rowHeight + 4m, PageRight - PageLeft, rowHeight, new PdfRgbColor(0.94m, 0.96m, 0.98m));
        }

        writer.WriteText(TableLabelX, y, labelLines[0], labelFontSize, line.IsEmphasized);
        for (var index = 1; index < labelLines.Count; index++)
        {
            writer.WriteText(TableLabelX, y - 2m - index * 8m, labelLines[index], writer.Style.CaptionFontSize, false, writer.Style.MutedTextColor);
        }

        writer.WriteText(TableQuantityX, y, line.QuantityDisplay, writer.Style.BodyFontSize, false);
        writer.WriteText(TableRateX, y, line.RateDisplay, writer.Style.BodyFontSize, false);
        writer.WriteTextRight(TableAmountRightX, y, line.AmountDisplay, writer.Style.BodyFontSize, line.IsEmphasized);

        if (line.IsEmphasized)
        {
            writer.DrawLine(PageLeft, y - rowHeight + 2m, PageRight, y - rowHeight + 2m, 0.5m);
        }

        return y - rowHeight;
    }

    private static void WriteAddressBlock(PdfPageWriter writer, decimal x, ref decimal y, string companyAddress, bool boldFirstLine)
    {
        var lines = SplitLines(companyAddress);
        if (lines.Count == 0)
        {
            return;
        }

        for (var index = 0; index < lines.Count; index++)
        {
            writer.WriteText(x, y, lines[index], writer.Style.BodyFontSize + 1m, boldFirstLine && index == 0);
            y -= 16m;
        }
    }

    private static void WriteTextBlock(PdfPageWriter writer, decimal x, decimal y, string text)
    {
        foreach (var line in SplitLines(text))
        {
            writer.WriteText(x, y, line, writer.Style.BodyFontSize + 1m, false);
            y -= 16m;
        }
    }

    private static void WriteMetaRow(PdfPageWriter writer, decimal y, PayrollStatementTemplateMetaRow? row)
    {
        if (row is null)
        {
            return;
        }

        WriteMetaBlock(writer, 40m, y, 148m, 122m, row.LeftLabel, row.LeftValue);
        WriteMetaBlock(writer, 320m, y, 112m, 118m, row.RightLabel, row.RightValue);
    }

    private static void WriteMetaBlock(PdfPageWriter writer, decimal x, decimal y, decimal labelWidth, decimal valueWidth, string label, string value)
    {
        var labelLines = WrapTextToWidth(label, labelWidth, writer.Style.BodyFontSize).ToArray();
        var valueLines = WrapTextToWidth(value, valueWidth, writer.Style.BodyFontSize).ToArray();

        for (var index = 0; index < labelLines.Length; index++)
        {
            writer.WriteText(x, y - index * 10m, labelLines[index], writer.Style.BodyFontSize, true);
        }

        for (var index = 0; index < valueLines.Length; index++)
        {
            writer.WriteText(x + labelWidth + 10m, y - index * 10m, valueLines[index], writer.Style.BodyFontSize, false);
        }
    }

    private static IReadOnlyList<string> SplitLines(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static IEnumerable<string> WrapTextToWidth(string? text, decimal maxWidth, decimal fontSize)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var remaining = text.Trim();
        while (PdfPageWriter.EstimateTextWidth(remaining, fontSize) > maxWidth)
        {
            var splitIndex = remaining.Length;
            while (splitIndex > 1 && PdfPageWriter.EstimateTextWidth(remaining[..splitIndex], fontSize) > maxWidth)
            {
                splitIndex = remaining.LastIndexOf(' ', splitIndex - 1);
                if (splitIndex <= 0)
                {
                    splitIndex = Math.Min(remaining.Length, Math.Max(1, (int)(maxWidth / Math.Max(1m, fontSize * 0.45m))));
                    break;
                }
            }

            yield return remaining[..splitIndex].TrimEnd();
            remaining = remaining[splitIndex..].TrimStart();
        }

        if (remaining.Length > 0)
        {
            yield return remaining;
        }
    }

    private static string Fallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string GetExportDirectory()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documentsPath))
        {
            return Path.Combine(documentsPath, AppDataDirectoryName, ExportDirectoryName);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            return Path.Combine(localAppData, AppDataDirectoryName, ExportDirectoryName);
        }

        return Path.Combine(AppContext.BaseDirectory, ExportDirectoryName);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "Lohnblatt" : sanitized;
    }

    private sealed class PdfPageWriter
    {
        private readonly StringBuilder _builder;
        public PdfStyleSettings Style { get; }

        public PdfPageWriter(StringBuilder builder, PdfStyleSettings style)
        {
            _builder = builder;
            Style = style;
        }

        public void WriteText(decimal x, decimal y, string text, decimal fontSize, bool bold)
        {
            WriteText(x, y, text, fontSize, bold, Style.TextColor);
        }

        public void WriteText(decimal x, decimal y, string text, decimal fontSize, bool bold, PdfRgbColor color)
        {
            _builder.AppendLine("BT");
            _builder.AppendLine($"/{(bold ? Style.BoldFontResourceName : Style.RegularFontResourceName)} {Format(fontSize)} Tf");
            _builder.AppendLine($"{Format(color.Red)} {Format(color.Green)} {Format(color.Blue)} rg");
            _builder.AppendLine($"{Format(x)} {Format(y)} Td");
            _builder.AppendLine($"({Escape(text)}) Tj");
            _builder.AppendLine("ET");
        }

        public void WriteTextRight(decimal rightX, decimal y, string text, decimal fontSize, bool bold)
        {
            var estimatedWidth = EstimateTextWidth(text, fontSize);
            WriteText(rightX - estimatedWidth, y, text, fontSize, bold);
        }

        public void DrawLine(decimal x1, decimal y1, decimal x2, decimal y2, decimal lineWidth)
        {
            _builder.AppendLine($"{Format(lineWidth)} w");
            _builder.AppendLine($"{Format(x1)} {Format(y1)} m");
            _builder.AppendLine($"{Format(x2)} {Format(y2)} l");
            _builder.AppendLine("S");
        }

        public void FillRectangle(decimal x, decimal y, decimal width, decimal height, PdfRgbColor color)
        {
            _builder.AppendLine($"{Format(color.Red)} {Format(color.Green)} {Format(color.Blue)} rg");
            _builder.AppendLine($"{Format(x)} {Format(y)} {Format(width)} {Format(height)} re");
            _builder.AppendLine("f");
        }

        public static decimal EstimateTextWidth(string text, decimal fontSize)
        {
            return Math.Max(0m, text.Length * fontSize * 0.48m);
        }

        private static string Escape(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }

        private static string Format(decimal value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }

    private static class PdfDocumentBuilder
    {
        public static byte[] Build(string contentStream, PdfStyleSettings style)
        {
            var encoding = Encoding.Latin1;
            var objects = new List<byte[]>();
            var contentBytes = encoding.GetBytes(contentStream);

            objects.Add(encoding.GetBytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"));
            objects.Add(encoding.GetBytes("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"));
            objects.Add(encoding.GetBytes("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>\nendobj\n"));
            objects.Add(encoding.GetBytes($"4 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{contentStream}\nendstream\nendobj\n"));
            objects.Add(encoding.GetBytes($"5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /{style.RegularFontBaseName} >>\nendobj\n"));
            objects.Add(encoding.GetBytes($"6 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /{style.BoldFontBaseName} >>\nendobj\n"));

            using var stream = new MemoryStream();
            var header = encoding.GetBytes("%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n");
            stream.Write(header, 0, header.Length);

            var offsets = new List<long> { 0 };
            foreach (var pdfObject in objects)
            {
                offsets.Add(stream.Position);
                stream.Write(pdfObject, 0, pdfObject.Length);
            }

            var xrefPosition = stream.Position;
            var xrefHeader = encoding.GetBytes($"xref\n0 {objects.Count + 1}\n");
            stream.Write(xrefHeader, 0, xrefHeader.Length);
            var freeEntry = encoding.GetBytes("0000000000 65535 f \n");
            stream.Write(freeEntry, 0, freeEntry.Length);

            foreach (var offset in offsets.Skip(1))
            {
                var line = encoding.GetBytes($"{offset:0000000000} 00000 n \n");
                stream.Write(line, 0, line.Length);
            }

            var trailer = encoding.GetBytes($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");
            stream.Write(trailer, 0, trailer.Length);
            return stream.ToArray();
        }
    }

    private readonly record struct PdfRgbColor(decimal Red, decimal Green, decimal Blue);

    private sealed record PayrollStatementTemplateMetaRow(
        string LeftLabel,
        string LeftValue,
        string RightLabel,
        string RightValue);

    private sealed record PayrollStatementTemplateTableHeader(
        string Label,
        string Quantity,
        string Rate,
        string Amount);

    private sealed record PayrollStatementTemplateClosingLine(
        string Text,
        bool IsBold);

    private abstract record PayrollStatementTemplateBodyItem;

    private sealed record PayrollStatementTemplateLineItem(
        PayrollStatementPdfLineDto Line) : PayrollStatementTemplateBodyItem;

    private sealed record PayrollStatementTemplateSpacerItem(
        decimal Height) : PayrollStatementTemplateBodyItem;

    private sealed class PayrollStatementTemplate
    {
        public string Logo { get; init; } = string.Empty;
        public string CompanyBlock { get; init; } = string.Empty;
        public string EmployeeBlock { get; init; } = string.Empty;
        public string AhvLine { get; init; } = string.Empty;
        public string BannerTitle { get; init; } = "Lohnblatt";
        public string BannerMonth { get; init; } = string.Empty;
        public PayrollStatementTemplateTableHeader TableHeader { get; init; } = new("Bezeichnung", "Einheit", "Ansatz", "Betrag");
        public IReadOnlyCollection<PayrollStatementTemplateMetaRow> MetaRows { get; init; } = Array.Empty<PayrollStatementTemplateMetaRow>();
        public IReadOnlyCollection<PayrollStatementTemplateBodyItem> BodyItems { get; init; } = Array.Empty<PayrollStatementTemplateBodyItem>();
        public IReadOnlyCollection<string> Notes { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<PayrollStatementTemplateClosingLine> ClosingLines { get; init; } = Array.Empty<PayrollStatementTemplateClosingLine>();

        public static PayrollStatementTemplate Parse(string templateContent, IReadOnlyDictionary<string, string> placeholders)
        {
            var metaRows = new List<PayrollStatementTemplateMetaRow>();
            var bodyItems = new List<PayrollStatementTemplateBodyItem>();
            var notes = new List<string>();
            var closingLines = new List<PayrollStatementTemplateClosingLine>();
            string logo = string.Empty;
            string companyBlock = string.Empty;
            string employeeBlock = string.Empty;
            string ahvLine = string.Empty;
            string bannerTitle = "Lohnblatt";
            string bannerMonth = string.Empty;
            var tableHeader = new PayrollStatementTemplateTableHeader("Bezeichnung", "Einheit", "Ansatz", "Betrag");

            foreach (var rawLine in templateContent.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                var trimmed = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var parts = trimmed.Split('|');
                if (parts.Length == 0)
                {
                    continue;
                }

                for (var index = 1; index < parts.Length; index++)
                {
                    parts[index] = ReplacePlaceholders(parts[index], placeholders);
                }

                switch (parts[0])
                {
                    case "LOGO":
                        logo = GetPart(parts, 1);
                        break;
                    case "COMPANY_BLOCK":
                        companyBlock = GetPart(parts, 1);
                        break;
                    case "EMPLOYEE_BLOCK":
                        employeeBlock = GetPart(parts, 1);
                        break;
                    case "AHV_LINE":
                        ahvLine = GetPart(parts, 1);
                        break;
                    case "BANNER":
                        bannerTitle = GetPart(parts, 1);
                        bannerMonth = GetPart(parts, 2);
                        break;
                    case "META_ROW":
                        metaRows.Add(new PayrollStatementTemplateMetaRow(
                            GetPart(parts, 1),
                            GetPart(parts, 2),
                            GetPart(parts, 3),
                            GetPart(parts, 4)));
                        break;
                    case "TABLE_HEADER":
                        tableHeader = new PayrollStatementTemplateTableHeader(
                            GetPart(parts, 1),
                            GetPart(parts, 2),
                            GetPart(parts, 3),
                            GetPart(parts, 4));
                        break;
                    case "PAYROLL_LINE":
                        bodyItems.Add(new PayrollStatementTemplateLineItem(new PayrollStatementPdfLineDto(
                            GetPart(parts, 2),
                            NormalizeDetail(GetPart(parts, 6)),
                            GetPart(parts, 3),
                            GetPart(parts, 4),
                            GetPart(parts, 5),
                            string.Equals(GetPart(parts, 1), "emphasis", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "SPACER":
                        bodyItems.Add(new PayrollStatementTemplateSpacerItem(ParseSpacerHeight(GetPart(parts, 1))));
                        break;
                    case "NOTES_BLOCK":
                        notes.AddRange(SplitLines(GetPart(parts, 1)).Where(line => !string.IsNullOrWhiteSpace(line) && line != "-"));
                        break;
                    case "CLOSING":
                        closingLines.Add(new PayrollStatementTemplateClosingLine(
                            GetPart(parts, 1),
                            closingLines.Count > 0));
                        break;
                }
            }

            return new PayrollStatementTemplate
            {
                Logo = logo,
                CompanyBlock = companyBlock,
                EmployeeBlock = employeeBlock,
                AhvLine = ahvLine,
                BannerTitle = bannerTitle,
                BannerMonth = bannerMonth,
                TableHeader = tableHeader,
                MetaRows = metaRows,
                BodyItems = bodyItems,
                Notes = notes,
                ClosingLines = closingLines
            };
        }

        private static string ReplacePlaceholders(string value, IReadOnlyDictionary<string, string> placeholders)
        {
            var resolved = value;
            foreach (var placeholder in placeholders)
            {
                resolved = resolved.Replace("{{" + placeholder.Key + "}}", placeholder.Value ?? string.Empty, StringComparison.Ordinal);
            }

            return resolved.Trim();
        }

        private static string GetPart(string[] parts, int index)
        {
            return index < parts.Length ? parts[index].Trim() : string.Empty;
        }

        private static string? NormalizeDetail(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == "-" ? null : value;
        }

        private static decimal ParseSpacerHeight(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedHeight)
                && parsedHeight > 0m)
            {
                return parsedHeight;
            }

            return 12m;
        }
    }

    private sealed class PdfStyleSettings
    {
        public string RegularFontBaseName { get; init; } = "Helvetica";
        public string BoldFontBaseName { get; init; } = "Helvetica-Bold";
        public string RegularFontResourceName => "F1";
        public string BoldFontResourceName => "F2";
        public decimal BodyFontSize { get; init; }
        public decimal CaptionFontSize { get; init; }
        public decimal TitleFontSize { get; init; }
        public PdfRgbColor TextColor { get; init; }
        public PdfRgbColor MutedTextColor { get; init; }
        public PdfRgbColor AccentColor { get; init; }

        public static PdfStyleSettings FromDocument(PayrollStatementPdfDocument document)
        {
            var baseFontSize = document.PrintFontSize <= 0m ? 9m : document.PrintFontSize;
            var (regularFont, boldFont) = ResolveBaseFonts(document.PrintFontFamily);

            return new PdfStyleSettings
            {
                RegularFontBaseName = regularFont,
                BoldFontBaseName = boldFont,
                BodyFontSize = baseFontSize,
                CaptionFontSize = Math.Max(7m, baseFontSize - 1m),
                TitleFontSize = baseFontSize + 1m,
                TextColor = ParseColor(document.PrintTextColorHex, 0m, 0m, 0m),
                MutedTextColor = ParseColor(document.PrintMutedTextColorHex, 0.35m, 0.42m, 0.48m),
                AccentColor = ParseColor(document.PrintAccentColorHex, 1m, 1m, 0m)
            };
        }

        private static (string RegularFont, string BoldFont) ResolveBaseFonts(string? fontFamily)
        {
            var normalized = fontFamily?.Trim().ToLowerInvariant() ?? string.Empty;
            if (normalized.Contains("courier", StringComparison.Ordinal))
            {
                return ("Courier", "Courier-Bold");
            }

            if (normalized.Contains("times", StringComparison.Ordinal))
            {
                return ("Times-Roman", "Times-Bold");
            }

            return ("Helvetica", "Helvetica-Bold");
        }

        private static PdfRgbColor ParseColor(string? colorHex, decimal fallbackRed, decimal fallbackGreen, decimal fallbackBlue)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(colorHex))
                {
                    return new PdfRgbColor(fallbackRed, fallbackGreen, fallbackBlue);
                }

                var value = colorHex.Trim().TrimStart('#');
                if (value.Length == 8)
                {
                    value = value[2..];
                }

                if (value.Length != 6)
                {
                    return new PdfRgbColor(fallbackRed, fallbackGreen, fallbackBlue);
                }

                var red = Convert.ToInt32(value[..2], 16) / 255m;
                var green = Convert.ToInt32(value.Substring(2, 2), 16) / 255m;
                var blue = Convert.ToInt32(value.Substring(4, 2), 16) / 255m;
                return new PdfRgbColor(red, green, blue);
            }
            catch
            {
                return new PdfRgbColor(fallbackRed, fallbackGreen, fallbackBlue);
            }
        }
    }
}
