namespace Forma.Blazor.Components.DataView;

/// <summary>What the checked selection's master control reaches when pressed.</summary>
public enum SelectAllMode
{
    /// <summary>No master control is drawn.</summary>
    None,

    /// <summary>The control selects or clears the whole set at one press, or the items on screen where the data view does not hold the set.</summary>
    Direct,

    /// <summary>The control selects or clears the items on screen, and a menu beside it offers the page, the whole set where the data view holds it, and clearing the selection.</summary>
    FromMenu,

    /// <summary>The control selects or clears the items on screen, and once they are all selected and the set holds more, the footer offers selecting the whole set.</summary>
    FromPager,
}
