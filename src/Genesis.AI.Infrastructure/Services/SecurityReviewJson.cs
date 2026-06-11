using System.Text.Json;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Shared JSON reading and validation primitives for the security review report
/// pipeline. Used by both the schema validator and the workbook writer.
/// </summary>
internal static class SecurityReviewJson
{
    public static JsonElement RequireObjectProperty(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"Missing required object: {propertyName}");
        }

        return property;
    }

    public static JsonElement RequireArrayProperty(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"Missing required array: {propertyName}");
        }

        if (property.GetArrayLength() == 0)
        {
            throw new InvalidOperationException($"Missing required array values: {propertyName}");
        }

        return property;
    }

    public static void RequireObject(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Expected JSON object at {name}");
    }

    public static void RequireAllowedProperties(JsonElement element, string name, IReadOnlyCollection<string> allowedProperties)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!allowedProperties.Contains(property.Name))
            {
                throw new InvalidOperationException($"Unexpected property in {name}: {property.Name}");
            }
        }
    }

    public static void RequireStringArray(JsonElement parent, string propertyName)
    {
        var array = RequireArrayProperty(parent, propertyName);
        foreach (var value in array.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new InvalidOperationException($"Invalid string array value in {propertyName}");
            }
        }
    }

    public static void RequireOptionalStringArray(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
            return;

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"Expected array property: {propertyName}");
        }

        foreach (var value in property.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new InvalidOperationException($"Invalid string array value in {propertyName}");
            }
        }
    }

    public static string RequireString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Missing required string: {propertyName}");
        }

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing required string: {propertyName}");

        return value;
    }

    public static void RequireOptionalString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
            return;

        if (property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException($"Invalid string property: {propertyName}");
        }
    }

    public static string RequireEnumValue(JsonElement parent, string propertyName, HashSet<string> allowedValues)
    {
        var value = RequireString(parent, propertyName);
        if (!allowedValues.Contains(value))
            throw new InvalidOperationException($"Invalid value for {propertyName}: {value}");

        return value;
    }

    public static string GetRequiredString(JsonElement element, string propertyName)
    {
        return RequireString(element, propertyName);
    }

    public static string OptString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return string.Empty;

        return property.GetString() ?? string.Empty;
    }

    public static string JoinArray(JsonElement array)
    {
        return string.Join(
            ", ",
            array
                .EnumerateArray()
                .Select(item => item.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!));
    }

    public static string JoinOptionalArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array || property.GetArrayLength() == 0)
            return string.Empty;

        return JoinArray(property);
    }
}
