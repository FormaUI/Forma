namespace Forma.Blazor.Components.Input;

/// <summary>An option offered by a select-style component: a value paired with its display label.</summary>
public sealed record SelectOption<TValue>(TValue Value, string Label);
