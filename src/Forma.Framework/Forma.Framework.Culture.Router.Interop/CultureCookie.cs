namespace Forma.Framework.Culture.Router.Interop;

using Forma.Framework.JavaScript.Interop;

/// <summary>The culture cookie, written from the browser.</summary>
public sealed class CultureCookie : IAsyncDisposable
{
    private const string ModulePath = "./_content/forma/culture-router/scripts/CultureCookie.js";

    private readonly JSModule _module;

    /// <summary>Creates the culture cookie over the JS environment <paramref name="js"/>.</summary>
    public CultureCookie(IJSEnvironment js)
    {
        ArgumentNullException.ThrowIfNull(js);

        _module = new JSModule(js, ModulePath);
    }

    /// <summary>Writes the cookie <paramref name="name"/> holding <paramref name="token"/> at <paramref name="path"/> for <paramref name="lifetime"/>, reporting whether the browser wrote it.</summary>
    /// <param name="name">The cookie's name.</param>
    /// <param name="token">The culture token the cookie holds.</param>
    /// <param name="path">The path the cookie is written at.</param>
    /// <param name="lifetime">How long the browser keeps the cookie.</param>
    /// <param name="isSecure">Whether the cookie is sent over HTTPS alone.</param>
    /// <returns>Whether the browser wrote the cookie.</returns>
    public async Task<bool> WriteAsync(string name, string token, string path, TimeSpan lifetime, bool isSecure)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(token);
        ArgumentException.ThrowIfNullOrEmpty(path);

        return await _module.TryInvokeVoidAsync("write", name, token, path, (long)lifetime.TotalSeconds, isSecure).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
