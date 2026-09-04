namespace Forma.Blazor.Features.Input.Pointer;

/// <summary>The part of a pointer's contact a binding matches.</summary>
public enum PointerPhase
{
    /// <summary>The press.</summary>
    Down,

    /// <summary>The release.</summary>
    Up,

    /// <summary>A move while in contact or hovering.</summary>
    Move,
}
