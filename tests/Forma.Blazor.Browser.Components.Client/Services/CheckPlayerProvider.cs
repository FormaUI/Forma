namespace Forma.Blazor.Browser.Components.Client.Services;

using Forma.Framework.Media;

/// <summary>A player provider resolving the checks' own <c>check:</c> addresses, standing in for a hosting service: its markup and the script that renders it for <c>check:markup</c>, and a page of the host's own for every other.</summary>
public sealed class CheckPlayerProvider : IPlayerProvider
{
    private const string Scheme = "check:";
    private const string PlayerPage = "_check/ready";
    private const string MarkupSource = "check:markup";
    private const string MarkupScript = "_check/embed.js";

    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return ValueTask.FromResult<PlayerFrame?>(source switch
        {
            MarkupSource => new HtmlPlayerFrame(MarkupSource, "<blockquote class=\"check-post\">A post</blockquote>", MarkupScript),
            _ when source.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase) => new EmbeddedPlayerFrame(PlayerPage),
            _ => null,
        });
    }
}
