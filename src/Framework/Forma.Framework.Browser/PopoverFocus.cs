namespace Forma.Framework.Browser;

/// <summary>Where the focus lands as a popover opens.</summary>
public enum PopoverFocus
{
    /// <summary>Where it stands, on the control that opened the popover.</summary>
    None,

    /// <summary>The first of the popover's controls it lands on.</summary>
    First,

    /// <summary>The last of the popover's controls it lands on.</summary>
    Last,
}
