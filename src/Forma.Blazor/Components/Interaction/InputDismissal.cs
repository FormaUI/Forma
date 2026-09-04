namespace Forma.Blazor.Components.Interaction;

using Forma.Framework.Input;

/// <summary>A dismissal the input router delivered to a scope while it was foremost.</summary>
/// <param name="Action">What the scope declared for the reason: dismiss or cancel.</param>
/// <param name="Reason">What asked the scope to go.</param>
public sealed record InputDismissal(DismissBehavior Action, DismissReason Reason);
