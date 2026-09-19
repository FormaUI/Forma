namespace Forma.Tests.Blazor;

using Bunit;

using Microsoft.AspNetCore.Components;

/// <summary>Lets a test hand a component the completion it is awaiting where the component will act on it at once, so no assertion depends on how quickly a continuation is scheduled.</summary>
public static class RenderedComponentExtensions
{
    /// <summary>Runs a release on the renderer, such as completing a gate a fake holds, and returns once the component has acted on it.</summary>
    /// <remarks>The component's continuation runs inline where the gate is completed, which is the renderer here, so the release must complete a plain <see cref="TaskCompletionSource"/>; one created to run its continuations asynchronously would queue the continuation instead, and the component would act after this returns.</remarks>
    /// <typeparam name="TComponent">The rendered component's type.</typeparam>
    /// <param name="cut">The rendered component.</param>
    /// <param name="release">What lets the component continue.</param>
    public static Task ReleaseAsync<TComponent>(this IRenderedComponent<TComponent> cut, Action release)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(cut);
        ArgumentNullException.ThrowIfNull(release);

        return cut.InvokeAsync(release);
    }
}
