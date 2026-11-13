namespace Forma.Framework.Resources;

/// <summary>A store's refusal of a file, and the limit the file went past.</summary>
/// <param name="Reason">Why the store refused the file.</param>
/// <param name="Limit">The limit the file went past, in bytes for <see cref="FileRefusalReason.TooLarge"/> and in pixels for <see cref="FileRefusalReason.TooWide"/> and <see cref="FileRefusalReason.TooTall"/>.</param>
public sealed record FileRefusal(FileRefusalReason Reason, long? Limit = null);
