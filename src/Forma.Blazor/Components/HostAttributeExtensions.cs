namespace Forma.Blazor.Components;

/// <summary>Reads and merges the values a host supplied through a component's captured attributes.</summary>
internal static class HostAttributeExtensions
{
    /// <summary>The value a host supplied for an attribute, or null where it supplied none.</summary>
    public static string? GetHostValue(this IReadOnlyDictionary<string, object>? attributes, string name)
        => attributes is not null && attributes.TryGetValue(name, out var value)
            ? value?.ToString()
            : null;

    /// <summary>A component's own classes followed by any the host supplied, so a host class adds to the component's styling rather than replacing it.</summary>
    public static string MergeClass(this IReadOnlyDictionary<string, object>? attributes, params ReadOnlySpan<string?> ownClasses)
    {
        var merged = new List<string>(ownClasses.Length + 1);

        foreach (var ownClass in ownClasses)
        {
            if (!string.IsNullOrWhiteSpace(ownClass))
            {
                merged.Add(ownClass);
            }
        }

        var hostClass = attributes.GetHostValue("class");

        if (!string.IsNullOrWhiteSpace(hostClass))
        {
            merged.Add(hostClass);
        }

        return string.Join(' ', merged);
    }
}
