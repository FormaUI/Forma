namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;

/// <summary>An event handler whose event draws nothing again by itself, leaving any drawing to what the handler asks for.</summary>
/// <remarks>
/// An event draws again the component that declares its handler, unless the handler's target is a receiver of its own; this one
/// receives the event and runs the handler alone. The renderer finds an error boundary through the component receiving the event,
/// which this receiver is not, so a failure is handed back to the declaring component to raise as its own.
/// </remarks>
internal static class NonRenderingHandler
{
    /// <summary>The callback that runs <paramref name="handler"/> on its event and draws nothing again by itself.</summary>
    /// <typeparam name="TValue">The event's arguments.</typeparam>
    /// <param name="handler">The handler.</param>
    /// <param name="dispatchException">Raises a failure as the declaring component's own, its <c>DispatchExceptionAsync</c>.</param>
    public static EventCallback<TValue> Create<TValue>(Action handler, Func<Exception, Task> dispatchException)
    {
        var receiver = new Receiver(handler, dispatchException);

        return EventCallback.Factory.Create<TValue>(receiver, receiver.Invoke);
    }

    private sealed class Receiver(Action handler, Func<Exception, Task> dispatchException) : IHandleEvent
    {
        public void Invoke()
            => handler();

        public async Task HandleEventAsync(EventCallbackWorkItem item, object? arg)
        {
            try
            {
                await item.InvokeAsync(arg);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                await dispatchException(exception);
            }
        }
    }
}
