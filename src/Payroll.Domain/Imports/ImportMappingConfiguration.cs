using Payroll.Domain.Common;

namespace Payroll.Domain.Imports;

public sealed class ImportMappingConfiguration : AuditableEntity
{
    private ImportMappingConfiguration()
    {
        Name = string.Empty;
        Delimiter = ";";
        TextQualifier = "\"";
        FieldMappingsJson = "[]";
    }

    public ImportMappingConfiguration(
        ImportConfigurationType type,
        string name,
        string delimiter,
        bool fieldsEnclosed,
        string textQualifier,
        string fieldMappingsJson)
    {
        Type = type;
        Update(name, delimiter, fieldsEnclosed, textQualifier, fieldMappingsJson);
    }

    public ImportConfigurationType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Delimiter { get; private set; } = ";";
    public bool FieldsEnclosed { get; private set; }
    public string TextQualifier { get; private set; } = "\"";
    public string FieldMappingsJson { get; private set; } = "[]";

    public void Update(
        string name,
        string delimiter,
        bool fieldsEnclosed,
        string textQualifier,
        string fieldMappingsJson)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Delimiter = NormalizeSingleCharacter(delimiter, nameof(delimiter));
        TextQualifier = NormalizeSingleCharacter(textQualifier, nameof(textQualifier));
        FieldMappingsJson = string.IsNullOrWhiteSpace(fieldMappingsJson)
            ? "[]"
            : fieldMappingsJson.Trim();
        FieldsEnclosed = fieldsEnclosed;
        Touch();
    }

    private static string NormalizeSingleCharacter(string value, string parameterName)
    {
        var trimmed = Guard.AgainstNullOrWhiteSpace(value, parameterName).Trim();
        if (trimmed.Length != 1)
        {
            throw new ArgumentException($"{parameterName} must contain exactly one character.", parameterName);
        }

        return trimmed;
    }
}
