namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Holds a container at the tallest height its content has needed, so a short page does not shrink it.</summary>
internal sealed class HeightReservation(IJSRuntime js)
    : JsBehaviour(js, "./_content/Forma.UI/scripts/behaviours/HeightReservation.js")
{
    /// <summary>Attaches the reservation to a container holding a table.</summary>
    public ValueTask<bool> AttachAsync(ElementReference container)
        => AttachCoreAsync(container);

    /// <summary>Measures the rows currently laid out and holds the container at a full page of them.</summary>
    public ValueTask<bool> ReserveAsync(int pageSize)
        => InvokeAsync("reserve", pageSize);
}
