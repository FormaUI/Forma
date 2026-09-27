namespace Forma.Framework.Browser.Interop.Behaviors;

/// <summary>What a drag shows it will do over a drop zone.</summary>
public enum DropEffect
{
    /// <summary>The dragged data is copied to the zone.</summary>
    Copy,

    /// <summary>The dragged data is moved to the zone.</summary>
    Move,

    /// <summary>The zone takes a link to the dragged data.</summary>
    Link,
}
