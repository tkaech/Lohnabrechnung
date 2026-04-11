using System.Text;
using Payroll.Application.Imports;

namespace Payroll.Infrastructure.Imports;

public sealed class CsvImportFileReader : ICsvImportFileReader
{
    public async Task<CsvImportDocumentDto> ReadAsync(ReadCsvImportDocumentCommand command, CancellationToken cancellationToken)
    {
        if (!File.Exists(command.FilePath))
        {
            throw new FileNotFoundException("CSV-Datei wurde nicht gefunden.", command.FilePath);
        }

        var lines = await File.ReadAllLinesAsync(command.FilePath, cancellationToken);
        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        if (nonEmptyLines.Length == 0)
        {
            throw new InvalidOperationException("CSV-Datei ist leer.");
        }

        var delimiter = NormalizeSingleCharacter(command.Delimiter, nameof(command.Delimiter));
        var textQualifier = NormalizeSingleCharacter(command.TextQualifier, nameof(command.TextQualifier));
        var headers = ParseLine(nonEmptyLines[0], delimiter, command.FieldsEnclosed, textQualifier)
            .Select(item => item.Trim())
            .ToArray();

        if (headers.Length == 0 || headers.All(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("CSV-Datei enthaelt keine gueltigen Spaltennamen.");
        }

        var rows = new List<IReadOnlyDictionary<string, string>>();
        for (var index = 1; index < nonEmptyLines.Length; index++)
        {
            var values = ParseLine(nonEmptyLines[index], delimiter, command.FieldsEnclosed, textQualifier);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            {
                var header = headers[columnIndex];
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                row[header] = columnIndex < values.Count ? values[columnIndex] : string.Empty;
            }

            rows.Add(row);
        }

        return new CsvImportDocumentDto(headers, rows);
    }

    private static char NormalizeSingleCharacter(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 1)
        {
            throw new InvalidOperationException($"{parameterName} muss genau ein Zeichen enthalten.");
        }

        return value.Trim()[0];
    }

    private static List<string> ParseLine(string line, char delimiter, bool fieldsEnclosed, char textQualifier)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        char? activeTextQualifier = null;

        for (var index = 0; index < line.Length; index++)
        {
            var currentChar = line[index];

            if (fieldsEnclosed && activeTextQualifier.HasValue)
            {
                if (currentChar == activeTextQualifier.Value)
                {
                    if (index + 1 < line.Length && line[index + 1] == activeTextQualifier.Value)
                    {
                        current.Append(activeTextQualifier.Value);
                        index++;
                        continue;
                    }

                    if (IsQualifierClosingPosition(line, index, delimiter))
                    {
                        activeTextQualifier = null;
                        continue;
                    }
                }
            }

            if (fieldsEnclosed
                && activeTextQualifier is null
                && current.Length == 0
                && IsSupportedTextQualifier(currentChar, textQualifier))
            {
                activeTextQualifier = currentChar;
                continue;
            }

            if (!activeTextQualifier.HasValue && currentChar == delimiter)
            {
                fields.Add(NormalizeFieldValue(current.ToString()));
                current.Clear();
                continue;
            }

            current.Append(currentChar);
        }

        fields.Add(NormalizeFieldValue(current.ToString()));
        return fields;
    }

    private static bool IsSupportedTextQualifier(char value, char configuredTextQualifier)
    {
        return value == configuredTextQualifier || value == '"' || value == '\'';
    }

    private static bool IsQualifierClosingPosition(string line, int qualifierIndex, char delimiter)
    {
        return qualifierIndex == line.Length - 1 || line[qualifierIndex + 1] == delimiter;
    }

    private static string NormalizeFieldValue(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length >= 2)
        {
            var first = trimmed[0];
            var last = trimmed[^1];
            if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
            {
                return trimmed[1..^1];
            }
        }

        return trimmed;
    }
}
