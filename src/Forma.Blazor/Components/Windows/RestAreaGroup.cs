namespace Forma.Blazor.Components.Windows;

using Forma.Framework.Windows;

/// <summary>A cluster of handles in a <see cref="FormaRestArea"/>: the windows of one origin or one instance, under the group's accessible name.</summary>
internal sealed record RestAreaGroup(string? Name, IReadOnlyList<IWindow> Windows);
