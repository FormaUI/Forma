namespace Forma.Framework.Localization;

/// <summary>Marks a member holding a locale read for the type another member of the same instance names.</summary>
/// <param name="typeMemberName">The name of the member naming the type.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class LocaleSourceAttribute(string typeMemberName) : Attribute
{
    /// <summary>The name of the member naming the type the locale is read for.</summary>
    public string TypeMemberName { get; } = typeMemberName;
}
