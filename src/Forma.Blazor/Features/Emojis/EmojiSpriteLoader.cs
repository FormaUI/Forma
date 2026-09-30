namespace Forma.Blazor.Features.Emojis;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>Adopts emoji sprite sheets into the document, fetching each one once however many emoji draw from it.</summary>
/// <remarks>
/// Each sheet's load is held as one task that every caller asking for the sheet awaits. A load that fails is
/// dropped, so the next caller starts a new one. A navigation replaces the document's body, symbols included,
/// and clears every held load with it.
/// </remarks>
public sealed class EmojiSpriteLoader : IEmojiSpriteLoader, IAsyncDisposable, IDisposable
{
    private readonly Dictionary<string, Task<bool>> _loadsBySheet = [with(StringComparer.Ordinal)];
    private readonly JSEnvironment _environment;
    private readonly NavigationManager? _navigation;

    private Asset? _asset;

    /// <summary>Creates a loader adopting sheets through the circuit's script runtime.</summary>
    /// <param name="js">The script runtime the sheets are adopted through.</param>
    /// <param name="logger">The logger a failed load is reported to.</param>
    /// <param name="navigation">The navigation manager whose location changes clear the held loads.</param>
    public EmojiSpriteLoader(IJSRuntime js, ILogger<EmojiSpriteLoader> logger, NavigationManager? navigation = null)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentNullException.ThrowIfNull(logger);

        _environment = new JSEnvironment(js, logger, Guid.CreateVersion7());
        _navigation = navigation;
        _navigation?.LocationChanged += HandleLocationChanged;
    }

    /// <inheritdoc />
    public Task<bool> LoadAsync(string sheet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheet);

        return LoadCoreAsync(sheet);
    }

    private async Task<bool> LoadCoreAsync(string sheet)
    {
        // The task is held, so callers racing the first load await it.
        if (!_loadsBySheet.TryGetValue(sheet, out var load))
        {
            load = (_asset ??= new Asset(_environment)).LoadSpriteAsync(sheet).AsTask();
            _loadsBySheet[sheet] = load;
        }

        var arrived = await load;

        // Only the load that failed is forgotten, so a retry another caller already started stays shared.
        if (!arrived && _loadsBySheet.TryGetValue(sheet, out var current) && ReferenceEquals(current, load))
        {
            _loadsBySheet.Remove(sheet);
            EmojiSpriteLoaderLog.SpriteSheetDidNotLoad(_environment.Logger, sheet, _environment.RuntimeId);
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

    /// <inheritdoc />
    /// <remarks>Releases the navigation subscription alone; the interop asset is released by <see cref="DisposeAsync"/>.</remarks>
    public void Dispose()
        => _navigation?.LocationChanged -= HandleLocationChanged;

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
        => _loadsBySheet.Clear();
}
