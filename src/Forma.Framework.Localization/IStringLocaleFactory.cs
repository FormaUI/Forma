namespace Forma.Framework.Localization;

/// <summary>Creates locales for a resource source.</summary>
public interface IStringLocaleFactory
{
    /// <summary>A locale for the resource that belongs to a type.</summary>
    IStringLocale Create(Type resourceSource);

    /// <summary>A locale for the resource named by a base name under a location.</summary>
    IStringLocale Create(string baseName, string location);
}
