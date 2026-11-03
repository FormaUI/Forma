namespace Forma.Blazor.Browser.Tests;

using System.Reflection;

/// <summary>The browser context a component check in a web view acts through: the context every web view of the application shares, whose closing ends the check instead, taking back what it routed and granted and closing its web view.</summary>
public class WebViewCheckContext : DispatchProxy
{
    private IBrowserContext _context = default!;
    private Func<Task> _end = default!;

    /// <summary>Creates the context a check acts through over <paramref name="context"/>, which <paramref name="end"/> ends the check in.</summary>
    /// <param name="context">The context the application's web views share.</param>
    /// <param name="end">Ends the check: takes back what it routed and granted, and closes its web view.</param>
    /// <returns>The context the check acts through.</returns>
    public static IBrowserContext Create(IBrowserContext context, Func<Task> end)
    {
        var proxy = Create<IBrowserContext, WebViewCheckContext>();
        var check = (WebViewCheckContext)(object)proxy;
        check._context = context;
        check._end = end;

        return proxy;
    }

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

        return targetMethod.Name switch
        {
            nameof(IAsyncDisposable.DisposeAsync) => new ValueTask(_end()),
            nameof(IBrowserContext.CloseAsync) => _end(),
            _ => targetMethod.Invoke(_context, args),
        };
    }
}
