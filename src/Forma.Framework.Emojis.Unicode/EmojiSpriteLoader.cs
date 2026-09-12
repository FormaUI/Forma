namespace Forma.Framework.Emojis.Unicode;

using Forma.Framework.Common.Interop.Operations;
using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>Adopts emoji sprite sheets into the document, fetching each one once however many emoji draw from it.</summary>
/// <remarks>
/// A sprite draws from a symbol the document has to hold, and the reference alone fetches nothing. Sharing one
/// loader keeps a surface drawing hundreds of emoji from asking for the same sheet once per emoji. A navigation
/// replaces the document's body, symbols included, so what the loader remembers is forgotten with it.
/// </remarks>
public sealed class EmojiSpriteLoader : IAsyncDisposable, IDisposable
{
    private readonly Dictionary<string, Task<bool>> _loadsBySheet = new(StringComparer.Ordinal);
    private readonly JSEnvironment _environment;
    private readonly NavigationManager? _navigation;

    private Asset? _asset;

    public EmojiSpriteLoader(IJSRuntime js, ILogger<EmojiSpriteLoader> logger, NavigationManager? navigation = null)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentNullException.ThrowIfNull(logger);

        _environment = new JSEnvironment(js, logger, Guid.CreateVersion7());
        _navigation = navigation;
        _navigation?.LocationChanged += HandleLocationChanged;
    }

    /// <summary>Adopts a sheet's symbols into the document, reporting whether they arrived.</summary>
    /// <param name="sheet">The address of the sheet to load.</param>
    /// <returns>Whether the sheet's symbols are in the document.</returns>
    public Task<bool> LoadAsync(string sheet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheet);

        return LoadCoreAsync(sheet);
    }

    private async Task<bool> LoadCoreAsync(string sheet)
    {
        // The task is held rather than its result, so callers racing the first load await it instead of repeating it.
        if (!_loadsBySheet.TryGetValue(sheet, out var load))
        {
            load = (_asset ??= new Asset(_environment)).LoadSpriteAsync(sheet).AsTask();
            _loadsBySheet[sheet] = load;
        }

        var arrived = await load;

        // A sheet that did not arrive is forgotten rather than remembered as failed, so a surface asking later
        // retries a transient failure instead of leaving its emoji blank for the rest of the circuit. Only the
        // load that failed is forgotten, so a retry someone else already started stays shared.
        if (!arrived && _loadsBySheet.TryGetValue(sheet, out var current) && ReferenceEquals(current, load))
        {
            _loadsBySheet.Remove(sheet);
            EmojiLog.SpriteSheetDidNotLoad(_environment.Logger, sheet, _environment.RuntimeId);
        }

        return arrived;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _navigation?.LocationChanged -= HandleLocationChanged;

        if (_asset is not null)
        {
            await _asset.DisposeAsync();
        }
    }

    /// <summary>Releases the managed state alone; a scope disposing synchronously has no JS runtime left to talk to.</summary>
    public void Dispose()
        => _navigation?.LocationChanged -= HandleLocationChanged;

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
        => _loadsBySheet.Clear();
}
