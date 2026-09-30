namespace Forma.Tests.Blazor;

using Bunit;

using Microsoft.AspNetCore.Components;

/// <summary>Releases the completion a rendered component awaits on the renderer, where the component acts on it at once.</summary>
public static class RenderedComponentExtensions
{
    /// <summary>Runs a release on the renderer, such as completing a gate a fake holds, and returns once the component has acted on it.</summary>
    /// <remarks>The component's continuation runs inline where the gate is completed, here the renderer, when the release completes a plain <see cref="TaskCompletionSource"/>; one created to run its continuations asynchronously queues the continuation, and the component acts after this returns.</remarks>
    /// <typeparam name="TComponent">The rendered component's type.</typeparam>
    /// <param name="cut">The rendered component.</param>
    /// <param name="release">The action that lets the component continue.</param>
    public static Task ReleaseAsync<TComponent>(this IRenderedComponent<TComponent> cut, Action release)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(cut);
        ArgumentNullException.ThrowIfNull(release);

        return cut.InvokeAsync(release);
    }
}
