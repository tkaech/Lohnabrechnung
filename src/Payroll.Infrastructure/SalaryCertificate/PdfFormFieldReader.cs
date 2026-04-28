using Payroll.Application.SalaryCertificate;

namespace Payroll.Infrastructure.SalaryCertificate;

public sealed class PdfFormFieldReader : ISalaryCertificatePdfFormFieldReader
{
    public async Task<IReadOnlyCollection<string>> ReadFieldNamesAsync(
        string templatePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var fieldNames = PdfDocumentModel.Load(templatePath).FieldsByName.Keys
                .OrderBy(fieldName => fieldName, StringComparer.Ordinal)
                .ToArray();

            return fieldNames;
        }
        catch (InvalidOperationException)
        {
            var content = await File.ReadAllTextAsync(templatePath, cancellationToken);
            return ExtractFieldNames(content)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(fieldName => fieldName, StringComparer.Ordinal)
                .ToArray();
        }
    }

    internal static IReadOnlyCollection<string> ExtractFieldNames(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var fieldNames = new List<string>();
        var searchIndex = 0;
        while (searchIndex < content.Length)
        {
            var tokenIndex = content.IndexOf("/T", searchIndex, StringComparison.Ordinal);
            if (tokenIndex < 0)
            {
                break;
            }

            var valueIndex = SkipWhitespace(content, tokenIndex + 2);
            if (valueIndex >= content.Length)
            {
                break;
            }

            if (PdfDocumentModel.TryExtractName(content[tokenIndex..], "T", out var fieldName)
                && !string.IsNullOrWhiteSpace(fieldName))
            {
                fieldNames.Add(fieldName);
            }

            searchIndex = valueIndex + 1;
        }

        return fieldNames;
    }

    private static int SkipWhitespace(string content, int index)
    {
        while (index < content.Length && char.IsWhiteSpace(content[index]))
        {
            index++;
        }

        return index;
    }
}
