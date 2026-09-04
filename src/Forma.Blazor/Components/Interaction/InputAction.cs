namespace Forma.Blazor.Components.Interaction;

using Forma.Framework.Input;

/// <summary>An action the input router resolved to a scope, and the input that raised it.</summary>
/// <param name="Action">The action's name, as the route declared it.</param>
/// <param name="Input">The input that raised it, as its family describes it.</param>
public sealed record InputAction(string Action, InputInfo Input);
