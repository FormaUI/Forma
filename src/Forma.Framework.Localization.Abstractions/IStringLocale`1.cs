namespace Forma.Framework.Localization;

/// <summary>Provides translated strings for the resource base name derived from <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type whose name and namespace locate the resource.</typeparam>
public interface IStringLocale<out T> : IStringLocale;
