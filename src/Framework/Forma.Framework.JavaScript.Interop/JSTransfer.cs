namespace Forma.Framework.JavaScript.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.JSInterop;

/// <summary>A value the browser sent .NET through the transfer: inline, or a stream of its bytes.</summary>
/// <remarks>
/// The browser half carries a value inline while the call carrying it fits its budget, and past it as a JavaScript-to-.NET
/// stream, which Blazor sends in chunks under the hub's message limit: a text's own UTF-8 bytes, or any other value's
/// JSON. A read names the size its receiver accepts, in bytes, which decides both an inline value and a stream, so a
/// stream's own default limit never applies. The stream's reference is released once its stream has been read, since
/// WebAssembly pulls each chunk through it, and a stream refused or discarded is released unread.
/// </remarks>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class JSTransfer
{
    /// <summary>The value, where it travelled inline.</summary>
    [JsonPropertyName("value")]
    public JsonElement Value { get; init; }

    /// <summary>The stream of the value's bytes, where it travelled past the budget.</summary>
    [JsonPropertyName("stream")]
    public IJSStreamReference? Stream { get; init; }

    /// <summary>Whether <see cref="Stream"/> carries a text's own bytes rather than a value's JSON.</summary>
    [JsonPropertyName("text")]
    public bool IsText { get; init; }

    /// <summary>Reads the text the transfer carries, at most <paramref name="maximumLength"/> bytes of UTF-8.</summary>
    /// <param name="maximumLength">The size the receiver accepts, in bytes.</param>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    /// <returns>The text, which is <see langword="null"/> where the browser sent nothing.</returns>
    /// <exception cref="JSTransferException">The text is longer than the receiver accepts, or its stream broke.</exception>
    public async ValueTask<string?> ReadTextAsync(long maximumLength, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumLength);

        if (Stream is null)
        {
            var text = Value.ValueKind is JsonValueKind.String ? Value.GetString() : null;

            return text is null || Encoding.UTF8.GetByteCount(text) <= maximumLength
                ? text
                : throw JSTransferException.Refused(Encoding.UTF8.GetByteCount(text), maximumLength);
        }

        if (!IsText)
        {
            return (await ReadJsonAsync(maximumLength, cancellationToken).ConfigureAwait(false)).GetString();
        }

        return await ReadStreamAsync(maximumLength, ReadToEndAsync, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads the value the transfer carries as JSON, at most <paramref name="maximumLength"/> bytes of it.</summary>
    /// <param name="maximumLength">The size the receiver accepts, in bytes.</param>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    /// <returns>The value.</returns>
    /// <exception cref="JSTransferException">The value is larger than the receiver accepts, or its stream broke.</exception>
    public async ValueTask<JsonElement> ReadJsonAsync(long maximumLength, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumLength);

        if (Stream is null)
        {
            var length = Encoding.UTF8.GetByteCount(Value.GetRawText());

            return length <= maximumLength ? Value : throw JSTransferException.Refused(length, maximumLength);
        }

        return await ReadStreamAsync<JsonElement>(maximumLength, IsText ? ReadTextAsJsonAsync : ParseAsync, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Discards the value unread, releasing the stream it travels in.</summary>
    public async ValueTask DiscardAsync()
    {
        if (Stream is { } reference)
        {
            await ReleaseAsync(reference).ConfigureAwait(false);
        }
    }

    // A stream past the accepted size is refused before a byte of it is read.
    private async ValueTask<TResult> ReadStreamAsync<TResult>(long maximumLength, Func<Stream, CancellationToken, ValueTask<TResult>> read, CancellationToken cancellationToken)
    {
        var reference = Stream!;

        try
        {
            if (reference.Length > maximumLength)
            {
                throw JSTransferException.Refused(reference.Length, maximumLength);
            }

            await using var stream = await reference.OpenReadStreamAsync(maximumLength, cancellationToken).ConfigureAwait(false);

            return await read(stream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsBreak(ex))
        {
            throw JSTransferException.Broken(ex);
        }
        finally
        {
            await ReleaseAsync(reference).ConfigureAwait(false);
        }
    }

    private static async ValueTask<string?> ReadToEndAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<JsonElement> ReadTextAsJsonAsync(Stream stream, CancellationToken cancellationToken)
        => JsonSerializer.SerializeToElement(await ReadToEndAsync(stream, cancellationToken).ConfigureAwait(false), JSTransferJsonContext.Default.String);

    private static async ValueTask<JsonElement> ParseAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        return document.RootElement.Clone();
    }

    // A release that doesn't reach the browser has nothing left to release there, its runtime having gone or failed the call.
    private static async ValueTask ReleaseAsync(IJSStreamReference reference)
    {
        try
        {
            await reference.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException)
        {
        }
    }

    // What a stream answers when it stalls, ends short, carries what doesn't parse or its runtime goes away.
    private static bool IsBreak(Exception exception)
        => exception is IOException or JsonException or InvalidOperationException or TimeoutException or JSException or JSDisconnectedException;
}

/// <summary>The JSON the transfer writes a streamed text into, generated so trimming keeps it.</summary>
[JsonSerializable(typeof(string))]
internal sealed partial class JSTransferJsonContext : JsonSerializerContext;
