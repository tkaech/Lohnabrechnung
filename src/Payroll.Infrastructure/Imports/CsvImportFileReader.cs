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

        var configuredDelimiter = NormalizeSingleCharacter(command.Delimiter, nameof(command.Delimiter));
        _ = NormalizeSingleCharacter(command.TextQualifier, nameof(command.TextQualifier));
        var delimiter = DetectDelimiter(nonEmptyLines, configuredDelimiter);
        var headers = ParseLine(nonEmptyLines[0], delimiter)
            .Select(item => item.Trim())
            .ToArray();

        if (headers.Length == 0 || headers.All(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("CSV-Datei enthaelt keine gueltigen Spaltennamen.");
        }

        var rows = new List<IReadOnlyDictionary<string, string>>();
        for (var index = 1; index < nonEmptyLines.Length; index++)
        {
            var values = ParseLine(nonEmptyLines[index], delimiter);
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

    private static char DetectDelimiter(IReadOnlyList<string> lines, char configuredDelimiter)
    {
        var candidates = new List<char> { '\t', ';', ',', configuredDelimiter }
            .Distinct()
            .ToArray();

        var sampleLines = lines.Take(5).ToArray();
        var scoredCandidates = candidates
            .Select(candidate => new
            {
                Delimiter = candidate,
                Score = ScoreDelimiter(sampleLines, candidate)
            })
            .ToArray();

        var detected = scoredCandidates
            .Where(item => item.Score.HeaderColumns > 1)
            .OrderByDescending(item => item.Score.HeaderColumns)
            .ThenByDescending(item => item.Score.MultiColumnLines)
            .ThenByDescending(item => item.Score.AverageColumns)
            .Select(item => item.Delimiter)
            .FirstOrDefault();

        return detected == default ? configuredDelimiter : detected;
    }

    private static (int HeaderColumns, int MultiColumnLines, decimal AverageColumns) ScoreDelimiter(
        IReadOnlyCollection<string> lines,
        char delimiter)
    {
        var counts = lines.Select(line => ParseLine(line, delimiter).Count).ToArray();
        if (counts.Length == 0)
        {
            return (0, 0, 0m);
        }

        return (
            counts[0],
            counts.Count(count => count > 1),
            Convert.ToDecimal(counts.Average(count => count)));
    }

    private static char NormalizeSingleCharacter(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 1)
        {
            throw new InvalidOperationException($"{parameterName} muss genau ein Zeichen enthalten.");
        }

        return value.Trim()[0];
    }

    private static List<string> ParseLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        char? activeTextQualifier = null;

        for (var index = 0; index < line.Length; index++)
        {
            var currentChar = line[index];

            if (activeTextQualifier.HasValue)
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

            if (activeTextQualifier is null
                && current.Length == 0
                && IsSupportedTextQualifier(currentChar))
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

    private static bool IsSupportedTextQualifier(char value)
    {
        return value == '"' || value == '\'';
    }

    private static bool IsQualifierClosingPosition(string line, int qualifierIndex, char delimiter)
    {
        for (var index = qualifierIndex + 1; index < line.Length; index++)
        {
            var currentChar = line[index];
            if (currentChar == delimiter)
            {
                return true;
            }

            if (!char.IsWhiteSpace(currentChar))
            {
                return false;
            }
        }

        return true;
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
