namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Css;

/// <summary>A color <see cref="TypographyPlugin"/> offers, with the name a reader knows it by.</summary>
/// <param name="Name">The color's English name, which the composer localizes as its option's label and accessible name.</param>
/// <param name="Color">The color the text or its highlight takes.</param>
public sealed record TypographyColor(string Name, CssColor Color);
