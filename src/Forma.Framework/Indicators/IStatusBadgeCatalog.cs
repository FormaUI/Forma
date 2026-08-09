namespace Forma.Framework.Indicators;

/// <summary>The status badges an application defines.</summary>
public interface IStatusBadgeCatalog : IEnumerable<StatusBadgeStyle>
{
    /// <summary>Every registered status badge.</summary>
    IReadOnlyCollection<StatusBadgeStyle> Statuses { get; }

    /// <summary>Registers a status badge, replacing any entry with the same name.</summary>
    void Add(StatusBadgeStyle status);

    /// <summary>Returns the badge registered under a name, or null when the name is unknown.</summary>
    StatusBadgeStyle? Find(string name);
}
