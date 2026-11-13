namespace Forma.Blazor.Components;

/// <summary>The element ids of a field's label, of its description and of the container holding its validation messages.</summary>
public sealed class FieldElementIds
{
    /// <summary>Creates the ids, unique to this instance.</summary>
    public FieldElementIds()
    {
        var stem = $"forma-{Guid.CreateVersion7():N}";
        LabelId = $"{stem}-label";
        DescriptionId = $"{stem}-description";
        MessageContainerId = $"{stem}-messages";
    }

    /// <summary>The id of the field's label.</summary>
    public string LabelId { get; }

    /// <summary>The id of the field's description.</summary>
    public string DescriptionId { get; }

    /// <summary>The id of the element holding the field's validation messages.</summary>
    public string MessageContainerId { get; }
}
