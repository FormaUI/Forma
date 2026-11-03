namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Catalog.Queries;

/// <summary>The band a group of items falls in.</summary>
/// <remarks>Groups stand in the order their bands are declared in.</remarks>
public enum ExplorerGroupBand
{
    /// <summary>A kind of item, named by the group's kind label.</summary>
    Kind,

    /// <summary>Names beginning with a digit.</summary>
    NameDigits,

    /// <summary>Names beginning with A through H.</summary>
    NameAToH,

    /// <summary>Names beginning with I through P.</summary>
    NameIToP,

    /// <summary>Names beginning with Q through Z.</summary>
    NameQToZ,

    /// <summary>Names beginning with any other character.</summary>
    NameOther,

    /// <summary>Items with no size, such as folders.</summary>
    SizeUnspecified,

    /// <summary>Empty items.</summary>
    SizeEmpty,

    /// <summary>Up to 16 KB.</summary>
    SizeTiny,

    /// <summary>Up to 1 MB.</summary>
    SizeSmall,

    /// <summary>Up to 128 MB.</summary>
    SizeMedium,

    /// <summary>Up to 1 GB.</summary>
    SizeLarge,

    /// <summary>Up to 4 GB.</summary>
    SizeHuge,

    /// <summary>Over 4 GB.</summary>
    SizeGigantic,

    /// <summary>Items with no date.</summary>
    DateUnspecified,

    /// <summary>Today.</summary>
    DateToday,

    /// <summary>Yesterday.</summary>
    DateYesterday,

    /// <summary>Earlier in the current week.</summary>
    DateEarlierThisWeek,

    /// <summary>In the week before the current one.</summary>
    DateLastWeek,

    /// <summary>Earlier in the current month.</summary>
    DateEarlierThisMonth,

    /// <summary>In the month before the current one.</summary>
    DateLastMonth,

    /// <summary>Earlier in the current year.</summary>
    DateEarlierThisYear,

    /// <summary>Before the current year.</summary>
    DateLongAgo,
}
