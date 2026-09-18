namespace Forma.Framework.Common.Interop.Behaviors;

/// <summary>The key a reader holds for a wheel turn to be answered.</summary>
public enum WheelModifier
{
    /// <summary>No key; every wheel turn is answered.</summary>
    None,

    /// <summary>The Shift key.</summary>
    Shift,

    /// <summary>The Ctrl key.</summary>
    Ctrl,

    /// <summary>The Alt key.</summary>
    Alt,
}
