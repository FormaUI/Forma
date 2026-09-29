namespace Forma.Blazor.Components.DataView;

/// <summary>The accessible name a caller gives a view, which the view's collection element carries rather than the root that wraps it with the view's states.</summary>
internal static class CollectionName
{
    private const string LabelAttribute = "aria-label";
    private const string LabelledByAttribute = "aria-labelledby";

    /// <summary>The name given as a label, if any.</summary>
    public static string? LabelOf(IReadOnlyDictionary<string, object>? attributes)
        => attributes?.GetValueOrDefault(LabelAttribute)?.ToString();

    /// <summary>The ids of the elements naming the collection, if any.</summary>
    public static string? LabelledByOf(IReadOnlyDictionary<string, object>? attributes)
        => attributes?.GetValueOrDefault(LabelledByAttribute)?.ToString();

    /// <summary>The caller's attributes with the name left out, for the root.</summary>
    public static IReadOnlyDictionary<string, object>? WithoutName(IReadOnlyDictionary<string, object>? attributes)
        => attributes is null || !(attributes.ContainsKey(LabelAttribute) || attributes.ContainsKey(LabelledByAttribute))
            ? attributes
            : attributes.Where(pair => pair.Key is not (LabelAttribute or LabelledByAttribute)).ToDictionary(pair => pair.Key, pair => pair.Value);
}
