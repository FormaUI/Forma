namespace Forma.Framework.Share;

using Forma.Framework.Icons;

/// <summary>The shell an API-backed target stands on: the connection it posts over, and where an item can be fetched from.</summary>
/// <param name="id">The unique identifier of the target.</param>
/// <param name="title">The target's displayed title.</param>
/// <param name="icon">The icon drawn with the title.</param>
/// <param name="http">The connection to post over, or null for one of the target's own.</param>
public abstract class ApiShare(string id, string title, Icon icon, HttpClient? http) : IShareTarget, IDisposable
{
    private readonly HttpClient _http = http ?? new HttpClient();
    private readonly bool _ownsHttp = http is null;

    /// <inheritdoc />
    public string Id { get; } = id;

    /// <inheritdoc />
    public string Title { get; } = title;

    /// <inheritdoc />
    public Icon Icon { get; } = icon;

    /// <summary>The connection the target posts over.</summary>
    protected HttpClient Http => _http;

    /// <inheritdoc />
    public abstract bool Supports(ShareItem item);

    /// <inheritdoc />
    public abstract Task ShareAsync(ShareItem item);

    /// <summary>The public address the item's resource can be fetched from, or null when it lives nowhere reachable.</summary>
    protected static string? GetReachableSource(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return Uri.TryCreate(item.Resource?.Source, UriKind.Absolute, out var source) && source.Scheme is "http" or "https"
            ? source.ToString()
            : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the connection when this target owns it.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _ownsHttp)
        {
            _http.Dispose();
        }
    }
}
