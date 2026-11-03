namespace Forma.Framework.Feedback;

/// <summary>The status badges an application defines.</summary>
public interface IStatusBadgeCatalog : IEnumerable<StatusBadgeStyle>
{
    /// <summary>Every registered status badge.</summary>
    IReadOnlyCollection<StatusBadgeStyle> Statuses { get; }

    /// <summary>Registers a status badge under its name.</summary>
    void Add(StatusBadgeStyle status);

    /// <summary>Returns the badge registered under a name.</summary>
    StatusBadgeStyle? Find(string name);
}
