namespace Forma.Tests.Interop;

using System.Text.Json;

/// <summary>Reads what an interop call sent to a script by the names the script reads it under.</summary>
public static class InteropPayload
{
    private static readonly JsonSerializerOptions WireOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>The names <paramref name="payload"/> sends, in the camel case its serialization writes.</summary>
    public static string[] GetNames(object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return [.. payload.GetType().GetProperties().Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))];
    }

    /// <summary>The JSON the interop writes for <paramref name="payload"/>.</summary>
    public static string ToJson(object? payload)
        => JsonSerializer.Serialize(payload, WireOptions);

    /// <summary>The value of the property the interop writes as <paramref name="name"/>, in the camel case its serialization writes.</summary>
    public static object? GetValue(object payload, string name)
        => TryGetValue(payload, name, out var value) ? value : throw new ArgumentException($"{payload.GetType().Name} sends no '{name}'.", nameof(name));

    /// <summary>Reads the value the interop writes as <paramref name="name"/>, reporting whether <paramref name="payload"/> sends one.</summary>
    public static bool TryGetValue(object payload, string name, out object? value)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var property = payload.GetType().GetProperties().SingleOrDefault(candidate => JsonNamingPolicy.CamelCase.ConvertName(candidate.Name) == name);
        value = property?.GetValue(payload);

        return property is not null;
    }
}
