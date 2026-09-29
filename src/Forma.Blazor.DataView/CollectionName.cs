namespace Forma.Blazor.Components.DataView;

/// <summary>The accessible name a caller gives a view, which the view's collection element carries rather than the root that wraps it with the view's states.</summary>
/// <param name="Label">The name given as a label, if any.</param>
/// <param name="LabelledBy">The ids of the elements naming the collection, if any.</param>
/// <param name="RootAttributes">The caller's attributes with the name left out, for the root.</param>
internal sealed record CollectionName(string? Label, string? LabelledBy, IReadOnlyDictionary<string, object>? RootAttributes)
{
    private const string LabelAttribute = "aria-label";
    private const string LabelledByAttribute = "aria-labelledby";

    /// <summary>No name and no attributes, before the caller's arrive.</summary>
    public static CollectionName None { get; } = new(null, null, null);

    /// <summary>Reads the name out of the attributes a caller gave the view.</summary>
    public static CollectionName From(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null)
        {
            return None;
        }

        var label = attributes.GetValueOrDefault(LabelAttribute)?.ToString();
        var labelledBy = attributes.GetValueOrDefault(LabelledByAttribute)?.ToString();
        var rootAttributes = label is null && labelledBy is null
            ? attributes
            : attributes.Where(pair => pair.Key is not (LabelAttribute or LabelledByAttribute)).ToDictionary(pair => pair.Key, pair => pair.Value);

        return new CollectionName(label, labelledBy, rootAttributes);
    }
}
