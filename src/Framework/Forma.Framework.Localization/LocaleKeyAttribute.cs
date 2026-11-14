namespace Forma.Framework.Localization;

/// <summary>Marks a parameter whose argument is a localization key, looked up in the locale of the call's type argument.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class LocaleKeyAttribute : Attribute;
