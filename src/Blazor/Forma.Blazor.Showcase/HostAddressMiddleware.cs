namespace Forma.Blazor.Showcase;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

/// <summary>Refuses with a 400 a request whose scheme, Host header and path base don't form an absolute address.</summary>
public sealed class HostAddressMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Creates the middleware ahead of the rest of the pipeline.</summary>
    public HostAddressMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        _next = next;
    }

    /// <summary>Passes on a request whose Host header forms an absolute address, and refuses any other.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.Request;

        if (!Uri.TryCreate(UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase), UriKind.Absolute, out _))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            return Task.CompletedTask;
        }

        return _next(context);
    }
}
