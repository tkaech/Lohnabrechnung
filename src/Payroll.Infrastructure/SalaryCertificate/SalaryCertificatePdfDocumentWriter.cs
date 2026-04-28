using System.Text;
using Payroll.Application.SalaryCertificate;

namespace Payroll.Infrastructure.SalaryCertificate;

public sealed class SalaryCertificatePdfDocumentWriter : ISalaryCertificatePdfDocumentWriter
{
    public async Task WriteAsync(
        string templatePath,
        string outputPath,
        IReadOnlyCollection<SalaryCertificatePdfFieldWriteDto> fields,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(fields);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException("Lohnausweis-Vorlage wurde nicht gefunden.", templatePath);
        }

        var templateFullPath = Path.GetFullPath(templatePath);
        var outputFullPath = Path.GetFullPath(outputPath);
        if (string.Equals(templateFullPath, outputFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Lohnausweis darf die Vorlage nicht ueberschreiben.");
        }

        var document = PdfDocumentModel.Load(templateFullPath);
        var updatedObjects = BuildUpdatedObjects(document, fields);

        Directory.CreateDirectory(Path.GetDirectoryName(outputFullPath) ?? AppContext.BaseDirectory);

        var builder = new StringBuilder();
        var offsets = new SortedDictionary<int, long>();
        long currentOffset = document.OriginalBytes.Length;

        foreach (var updatedObject in updatedObjects.OrderBy(item => item.Key))
        {
            var objectText = $"{updatedObject.Key} 0 obj\n{updatedObject.Value}\nendobj\n";
            offsets[updatedObject.Key] = currentOffset;
            currentOffset += Encoding.ASCII.GetByteCount(objectText);
            builder.Append(objectText);
        }

        var xrefOffset = currentOffset;
        builder.Append("xref\n");
        foreach (var group in offsets.Keys.GroupConsecutive())
        {
            builder.Append(group.Start).Append(' ').Append(group.Count).Append('\n');
            foreach (var objectNumber in group.ObjectNumbers)
            {
                builder.Append(offsets[objectNumber].ToString("D10"))
                    .Append(" 00000 n \n");
            }
        }

        var size = Math.Max(document.Objects.Keys.DefaultIfEmpty(0).Max(), offsets.Keys.DefaultIfEmpty(0).Max()) + 1;
        builder.Append("trailer\n<< /Size ")
            .Append(size)
            .Append(" /Root ")
            .Append(document.RootReference)
            .Append(" /Prev ")
            .Append(document.PreviousStartXref)
            .Append(" >>\nstartxref\n")
            .Append(xrefOffset)
            .Append("\n%%EOF\n");

        await using var output = new FileStream(outputFullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await output.WriteAsync(document.OriginalBytes, cancellationToken);
        var appendedBytes = Encoding.ASCII.GetBytes(builder.ToString());
        await output.WriteAsync(appendedBytes, cancellationToken);
    }

    private static IReadOnlyDictionary<int, string> BuildUpdatedObjects(
        PdfDocumentModel document,
        IReadOnlyCollection<SalaryCertificatePdfFieldWriteDto> fields)
    {
        var updatedObjects = new Dictionary<int, string>();
        foreach (var field in fields)
        {
            if (!document.FieldsByName.TryGetValue(field.PdfFieldName, out var fieldObject))
            {
                throw new InvalidOperationException($"PDF-Feld wurde in der Vorlage nicht gefunden: {field.PdfFieldName}");
            }

            var updatedContent = PdfDocumentModel.UpsertValueEntry(
                fieldObject.Content,
                "V",
                PdfDocumentModel.ToLiteralString(field.Value));
            updatedContent = PdfDocumentModel.UpsertValueEntry(
                updatedContent,
                "DV",
                PdfDocumentModel.ToLiteralString(field.Value));
            updatedObjects[fieldObject.ObjectNumber] = updatedContent;
        }

        if (!document.Objects.TryGetValue(document.AcroFormObjectNumber, out var acroFormObject))
        {
            throw new InvalidOperationException("AcroForm-Objekt wurde in der Vorlage nicht gefunden.");
        }

        updatedObjects[document.AcroFormObjectNumber] = PdfDocumentModel.EnsureBooleanEntry(
            acroFormObject.Content,
            "NeedAppearances",
            true);

        return updatedObjects;
    }
}

internal static class EnumerableExtensions
{
    public static IEnumerable<ConsecutiveGroup> GroupConsecutive(this IEnumerable<int> source)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            yield break;
        }

        var currentGroup = new List<int> { enumerator.Current };
        while (enumerator.MoveNext())
        {
            if (enumerator.Current == currentGroup[^1] + 1)
            {
                currentGroup.Add(enumerator.Current);
                continue;
            }

            yield return new ConsecutiveGroup(currentGroup[0], currentGroup);
            currentGroup = [enumerator.Current];
        }

        yield return new ConsecutiveGroup(currentGroup[0], currentGroup);
    }
}

internal sealed record ConsecutiveGroup(
    int Start,
    IReadOnlyCollection<int> ObjectNumbers)
{
    public int Count => ObjectNumbers.Count;
}
