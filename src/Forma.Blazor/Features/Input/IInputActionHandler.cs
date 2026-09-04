namespace Forma.Blazor.Features.Input;

/// <summary>Receives the actions of the routes a host declares in <see cref="InputRouterOptions"/>.</summary>
public interface IInputActionHandler
{
    /// <summary>Performs <paramref name="action"/> for the <paramref name="input"/> that raised it.</summary>
    Task HandleAsync(string action, InputInfo input);
}
