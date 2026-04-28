using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Payroll.Infrastructure.SalaryCertificate;

internal sealed class PdfDocumentModel
{
    private static readonly Regex IndirectObjectPattern = new(@"(?s)(\d+)\s+(\d+)\s+obj\b(.*?)endobj", RegexOptions.Compiled);
    private static readonly Regex ReferencePattern = new(@"(\d+)\s+(\d+)\s+R", RegexOptions.Compiled);

    private PdfDocumentModel(
        byte[] originalBytes,
        string content,
        int previousStartXref,
        string rootReference,
        int acroFormObjectNumber,
        IReadOnlyDictionary<int, PdfIndirectObject> objects,
        IReadOnlyDictionary<string, PdfFieldObject> fieldsByName)
    {
        OriginalBytes = originalBytes;
        Content = content;
        PreviousStartXref = previousStartXref;
        RootReference = rootReference;
        AcroFormObjectNumber = acroFormObjectNumber;
        Objects = objects;
        FieldsByName = fieldsByName;
    }

    public byte[] OriginalBytes { get; }

    public string Content { get; }

    public int PreviousStartXref { get; }

    public string RootReference { get; }

    public int AcroFormObjectNumber { get; }

    public IReadOnlyDictionary<int, PdfIndirectObject> Objects { get; }

    public IReadOnlyDictionary<string, PdfFieldObject> FieldsByName { get; }

    public static PdfDocumentModel Load(string templatePath)
    {
        var bytes = File.ReadAllBytes(templatePath);
        var content = Encoding.Latin1.GetString(bytes);

        var objects = ReadObjects(bytes, content);
        var rootReference = ReadRequiredGlobalReference(content, "Root");
        var acroFormReference = ReadRequiredGlobalReference(content, "AcroForm");
        var previousStartXref = ReadPreviousStartXref(content);
        var fieldsByName = objects.Values
            .Select(TryCreateFieldObject)
            .Where(field => field is not null)
            .Cast<PdfFieldObject>()
            .ToDictionary(field => field.FieldName, StringComparer.Ordinal);

        return new PdfDocumentModel(
            bytes,
            content,
            previousStartXref,
            rootReference,
            ParseReferenceObjectNumber(acroFormReference),
            objects,
            fieldsByName);
    }

    private static IReadOnlyDictionary<int, PdfIndirectObject> ReadObjects(byte[] bytes, string content)
    {
        var objects = new Dictionary<int, PdfIndirectObject>();

        foreach (Match match in IndirectObjectPattern.Matches(content))
        {
            var objectNumber = int.Parse(match.Groups[1].Value);
            var generation = int.Parse(match.Groups[2].Value);
            var objectContent = match.Groups[3].Value.Trim();

            objects[objectNumber] = new PdfIndirectObject(objectNumber, generation, objectContent);

            if (!IsObjectStream(objectContent))
            {
                continue;
            }

            foreach (var embeddedObject in ReadEmbeddedObjects(bytes, match, objectNumber))
            {
                if (!objects.ContainsKey(embeddedObject.ObjectNumber))
                {
                    objects[embeddedObject.ObjectNumber] = embeddedObject;
                }
            }
        }

        return objects;
    }

    private static IEnumerable<PdfIndirectObject> ReadEmbeddedObjects(byte[] bytes, Match objectMatch, int containerObjectNumber)
    {
        var objectContent = objectMatch.Groups[3].Value;
        var streamIndex = objectContent.IndexOf("stream", StringComparison.Ordinal);
        var endStreamIndex = objectContent.IndexOf("endstream", StringComparison.Ordinal);
        if (streamIndex < 0 || endStreamIndex < 0 || endStreamIndex <= streamIndex)
        {
            yield break;
        }

        var first = ReadRequiredInt(objectContent, "First");
        var count = ReadRequiredInt(objectContent, "N");
        var absoluteStreamStart = objectMatch.Groups[3].Index + streamIndex + "stream".Length;
        while (absoluteStreamStart < bytes.Length && (bytes[absoluteStreamStart] == '\r' || bytes[absoluteStreamStart] == '\n'))
        {
            absoluteStreamStart++;
        }

        var absoluteStreamEnd = objectMatch.Groups[3].Index + endStreamIndex;
        while (absoluteStreamEnd > absoluteStreamStart && (bytes[absoluteStreamEnd - 1] == '\r' || bytes[absoluteStreamEnd - 1] == '\n'))
        {
            absoluteStreamEnd--;
        }

        var streamBytes = bytes[absoluteStreamStart..absoluteStreamEnd];
        var decodedBytes = Inflate(streamBytes);
        var decodedContent = Encoding.Latin1.GetString(decodedBytes);
        if (decodedContent.Length < first)
        {
            throw new InvalidOperationException($"PDF-Objektstrom {containerObjectNumber} ist ungueltig.");
        }

        var header = decodedContent[..first];
        var body = decodedContent[first..];
        var headerTokens = header
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (headerTokens.Length < count * 2)
        {
            throw new InvalidOperationException($"PDF-Objektstrom {containerObjectNumber} enthaelt zu wenig Headerdaten.");
        }

        var entries = new List<(int ObjectNumber, int Offset)>(count);
        for (var index = 0; index < count; index++)
        {
            entries.Add((int.Parse(headerTokens[index * 2]), int.Parse(headerTokens[index * 2 + 1])));
        }

        for (var index = 0; index < entries.Count; index++)
        {
            var start = entries[index].Offset;
            var end = index + 1 < entries.Count ? entries[index + 1].Offset : body.Length;
            var embeddedContent = body[start..end].Trim();
            yield return new PdfIndirectObject(entries[index].ObjectNumber, 0, embeddedContent);
        }
    }

    private static byte[] Inflate(byte[] compressedBytes)
    {
        using var input = new MemoryStream(compressedBytes);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static bool IsObjectStream(string objectContent)
    {
        return objectContent.Contains("/Type/ObjStm", StringComparison.Ordinal)
            || objectContent.Contains("/Type /ObjStm", StringComparison.Ordinal);
    }

    private static PdfFieldObject? TryCreateFieldObject(PdfIndirectObject obj)
    {
        if (!TryExtractName(obj.Content, "T", out var fieldName) || string.IsNullOrWhiteSpace(fieldName))
        {
            return null;
        }

        return new PdfFieldObject(obj.ObjectNumber, obj.Generation, fieldName, obj.Content);
    }

    private static string ReadRequiredReference(string content, string name)
    {
        var keyIndex = FindDictionaryKeyIndex(content, name);
        if (keyIndex < 0)
        {
            throw new InvalidOperationException($"PDF-Referenz '/{name}' wurde nicht gefunden.");
        }

        var valueStart = SkipWhitespace(content, keyIndex + name.Length + 1);
        var referenceMatch = ReferencePattern.Match(content[valueStart..]);
        if (!referenceMatch.Success || referenceMatch.Index != 0)
        {
            throw new InvalidOperationException($"PDF-Referenz '/{name}' wurde nicht gefunden.");
        }

        return referenceMatch.Value;
    }

    private static string ReadRequiredGlobalReference(string content, string name)
    {
        var keyIndex = LastIndexOfAny(
            content,
            "/" + name + " ",
            "/" + name + "\r",
            "/" + name + "\n",
            "/" + name + "\t");
        if (keyIndex < 0)
        {
            throw new InvalidOperationException($"PDF-Referenz '/{name}' wurde nicht gefunden.");
        }

        var valueStart = SkipWhitespace(content, keyIndex + name.Length + 1);
        var referenceMatch = ReferencePattern.Match(content[valueStart..]);
        if (!referenceMatch.Success || referenceMatch.Index != 0)
        {
            throw new InvalidOperationException($"PDF-Referenz '/{name}' wurde nicht gefunden.");
        }

        return referenceMatch.Value;
    }

    private static int ReadPreviousStartXref(string content)
    {
        var matches = Regex.Matches(content, @"startxref\s+(\d+)", RegexOptions.Compiled);
        if (matches.Count == 0)
        {
            throw new InvalidOperationException("PDF startxref wurde nicht gefunden.");
        }

        return int.Parse(matches[^1].Groups[1].Value);
    }

    private static int ParseReferenceObjectNumber(string reference)
    {
        var match = ReferencePattern.Match(reference);
        if (!match.Success)
        {
            throw new InvalidOperationException($"PDF-Referenz ist ungueltig: {reference}");
        }

        return int.Parse(match.Groups[1].Value);
    }

    private static int ReadRequiredInt(string content, string name)
    {
        var match = Regex.Match(content, $@"/{Regex.Escape(name)}\s+(\d+)", RegexOptions.Compiled);
        if (!match.Success)
        {
            throw new InvalidOperationException($"PDF-Wert '/{name}' wurde nicht gefunden.");
        }

        return int.Parse(match.Groups[1].Value);
    }

    internal static bool TryExtractName(string content, string key, out string? value)
    {
        var keyIndex = FindDictionaryKeyIndex(content, key);
        if (keyIndex < 0)
        {
            value = null;
            return false;
        }

        var valueIndex = SkipWhitespace(content, keyIndex + key.Length + 1);
        if (valueIndex >= content.Length)
        {
            value = null;
            return false;
        }

        return TryReadStringToken(content, valueIndex, out value, out _);
    }

    internal static string UpsertValueEntry(string content, string key, string valueToken)
    {
        return UpsertEntry(content, key, valueToken);
    }

    internal static string EnsureBooleanEntry(string content, string key, bool value)
    {
        return UpsertEntry(content, key, value ? "true" : "false");
    }

    private static string UpsertEntry(string content, string key, string valueToken)
    {
        var keyIndex = FindDictionaryKeyIndex(content, key);
        if (keyIndex >= 0)
        {
            var valueIndex = SkipWhitespace(content, keyIndex + key.Length + 1);
            var endIndex = ReadTokenEnd(content, valueIndex);
            return content[..keyIndex] + "/" + key + " " + valueToken + content[endIndex..];
        }

        var insertIndex = content.LastIndexOf(">>", StringComparison.Ordinal);
        if (insertIndex < 0)
        {
            throw new InvalidOperationException("PDF-Dictionary konnte nicht aktualisiert werden.");
        }

        return content.Insert(insertIndex, $" /{key} {valueToken}");
    }

    private static int FindDictionaryKeyIndex(string content, string key)
    {
        var search = "/" + key;
        var index = 0;
        while (index < content.Length)
        {
            index = content.IndexOf(search, index, StringComparison.Ordinal);
            if (index < 0)
            {
                return -1;
            }

            var nextIndex = index + search.Length;
            if (nextIndex >= content.Length || char.IsWhiteSpace(content[nextIndex]) || IsDelimiter(content[nextIndex]))
            {
                return index;
            }

            index = nextIndex;
        }

        return -1;
    }

    private static int LastIndexOfAny(string content, params string[] candidates)
    {
        var bestIndex = -1;
        foreach (var candidate in candidates)
        {
            var index = content.LastIndexOf(candidate, StringComparison.Ordinal);
            if (index > bestIndex)
            {
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    internal static string ToLiteralString(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

        return "(" + escaped + ")";
    }

    private static bool TryReadStringToken(string content, int startIndex, out string? value, out int endIndex)
    {
        switch (content[startIndex])
        {
            case '(':
                return TryReadLiteralString(content, startIndex, out value, out endIndex);
            case '<':
                if (startIndex + 1 < content.Length && content[startIndex + 1] == '<')
                {
                    value = null;
                    endIndex = startIndex;
                    return false;
                }

                return TryReadHexString(content, startIndex, out value, out endIndex);
            default:
                value = null;
                endIndex = startIndex;
                return false;
        }
    }

    private static int SkipWhitespace(string content, int index)
    {
        while (index < content.Length && char.IsWhiteSpace(content[index]))
        {
            index++;
        }

        return index;
    }

    private static int ReadTokenEnd(string content, int startIndex)
    {
        if (startIndex >= content.Length)
        {
            return startIndex;
        }

        return content[startIndex] switch
        {
            '(' => ReadLiteralStringEnd(content, startIndex),
            '<' when startIndex + 1 < content.Length && content[startIndex + 1] == '<' => ReadDictionaryEnd(content, startIndex),
            '<' => ReadUntil(content, startIndex + 1, '>'),
            '[' => ReadArrayEnd(content, startIndex),
            '/' => ReadSimpleTokenEnd(content, startIndex + 1),
            _ => ReadSimpleOrReferenceEnd(content, startIndex)
        };
    }

    private static int ReadLiteralStringEnd(string content, int startIndex)
    {
        TryReadLiteralString(content, startIndex, out _, out var endIndex);
        return endIndex;
    }

    private static int ReadArrayEnd(string content, int startIndex)
    {
        var depth = 1;
        var index = startIndex + 1;
        while (index < content.Length && depth > 0)
        {
            switch (content[index])
            {
                case '\\':
                    index += 2;
                    continue;
                case '(':
                    index = ReadLiteralStringEnd(content, index);
                    continue;
                case '[':
                    depth++;
                    break;
                case ']':
                    depth--;
                    break;
                case '<' when index + 1 < content.Length && content[index + 1] == '<':
                    index = ReadDictionaryEnd(content, index);
                    continue;
            }

            index++;
        }

        return index;
    }

    private static int ReadDictionaryEnd(string content, int startIndex)
    {
        var depth = 1;
        var index = startIndex + 2;
        while (index < content.Length && depth > 0)
        {
            if (content[index] == '\\')
            {
                index += 2;
                continue;
            }

            if (content[index] == '(')
            {
                index = ReadLiteralStringEnd(content, index);
                continue;
            }

            if (content[index] == '<' && index + 1 < content.Length && content[index + 1] == '<')
            {
                depth++;
                index += 2;
                continue;
            }

            if (content[index] == '>' && index + 1 < content.Length && content[index + 1] == '>')
            {
                depth--;
                index += 2;
                continue;
            }

            index++;
        }

        return index;
    }

    private static int ReadUntil(string content, int startIndex, char endChar)
    {
        var index = startIndex;
        while (index < content.Length && content[index] != endChar)
        {
            index++;
        }

        return index < content.Length ? index + 1 : content.Length;
    }

    private static int ReadSimpleTokenEnd(string content, int startIndex)
    {
        var index = startIndex;
        while (index < content.Length && !char.IsWhiteSpace(content[index]) && !IsDelimiter(content[index]))
        {
            index++;
        }

        return index;
    }

    private static int ReadSimpleOrReferenceEnd(string content, int startIndex)
    {
        var index = ReadSimpleTokenEnd(content, startIndex);
        var whitespaceIndex = SkipWhitespace(content, index);
        if (whitespaceIndex > index
            && whitespaceIndex < content.Length
            && char.IsDigit(content[startIndex])
            && whitespaceIndex < content.Length
            && char.IsDigit(content[whitespaceIndex]))
        {
            var secondEnd = ReadSimpleTokenEnd(content, whitespaceIndex);
            var thirdStart = SkipWhitespace(content, secondEnd);
            if (thirdStart < content.Length && content[thirdStart] == 'R')
            {
                return thirdStart + 1;
            }
        }

        return index;
    }

    private static bool IsDelimiter(char character)
    {
        return character is '/' or '<' or '>' or '[' or ']' or '(' or ')';
    }

    private static bool TryReadLiteralString(string content, int startIndex, out string? value, out int endIndex)
    {
        var builder = new StringBuilder();
        var depth = 1;
        for (var index = startIndex + 1; index < content.Length; index++)
        {
            var character = content[index];
            if (character == '\\' && index + 1 < content.Length)
            {
                builder.Append(content[index + 1]);
                index++;
                continue;
            }

            if (character == '(')
            {
                depth++;
            }
            else if (character == ')')
            {
                depth--;
                if (depth == 0)
                {
                    value = builder.ToString();
                    endIndex = index + 1;
                    return true;
                }
            }

            builder.Append(character);
        }

        value = null;
        endIndex = content.Length;
        return false;
    }

    private static bool TryReadHexString(string content, int startIndex, out string? value, out int endIndex)
    {
        var closeIndex = content.IndexOf('>', startIndex + 1);
        if (closeIndex < 0)
        {
            value = null;
            endIndex = content.Length;
            return false;
        }

        var hex = new string(content[(startIndex + 1)..closeIndex]
            .Where(Uri.IsHexDigit)
            .ToArray());
        if (hex.Length == 0)
        {
            value = null;
            endIndex = closeIndex + 1;
            return false;
        }

        if (hex.Length % 2 == 1)
        {
            hex += "0";
        }

        var bytes = Enumerable.Range(0, hex.Length / 2)
            .Select(index => Convert.ToByte(hex.Substring(index * 2, 2), 16))
            .ToArray();

        value = Encoding.BigEndianUnicode.GetString(bytes);
        if (value.Length > 0 && value[0] != '\uFEFF' && bytes.All(item => item < 128))
        {
            value = Encoding.ASCII.GetString(bytes);
        }

        value = value.TrimStart('\uFEFF');
        endIndex = closeIndex + 1;
        return true;
    }
}

internal sealed record PdfIndirectObject(
    int ObjectNumber,
    int Generation,
    string Content);

internal sealed record PdfFieldObject(
    int ObjectNumber,
    int Generation,
    string FieldName,
    string Content);
