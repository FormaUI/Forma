namespace Forma.Framework.Themes;

/// <summary>Keeps the signed-in reader's theme preference, for every device they read on.</summary>
public interface IThemePreferenceStore
{
    /// <summary>Reads the preference kept for the reader.</summary>
    /// <param name="cancellationToken">Ends the read.</param>
    /// <returns>The preference as it was kept, or <see langword="null"/> where none is kept.</returns>
    ValueTask<ThemePreference?> ReadAsync(CancellationToken cancellationToken);

    /// <summary>Keeps <paramref name="preference"/> for the reader, in place of the one kept before.</summary>
    /// <param name="preference">The preference to keep, as it is to be read back.</param>
    /// <param name="cancellationToken">Ends the write.</param>
    /// <returns>A task that completes once the preference is kept.</returns>
    ValueTask WriteAsync(ThemePreference preference, CancellationToken cancellationToken);
}
