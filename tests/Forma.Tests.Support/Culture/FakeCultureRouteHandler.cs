namespace Forma.Tests.Culture;

using Forma.Framework.Culture;

/// <summary>A culture route handler that records every address it handles and handles each with the effect a test supplies.</summary>
/// <param name="handle">The effect each address is handled with.</param>
public sealed class FakeCultureRouteHandler(Func<Uri, Task> handle) : ICultureRouteHandler
{
    /// <summary>The addresses handled so far, in order.</summary>
    public List<Uri> Addresses { get; } = [];

    /// <inheritdoc />
    public Task HandleAsync(Uri address)
    {
        Addresses.Add(address);

        return handle(address);
    }
}
