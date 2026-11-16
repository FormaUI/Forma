namespace Forma.Blazor.Components;

/// <summary>Makes the contexts a user scope's Forma components are given.</summary>
/// <remarks>Making a context initializes the user scope's culture support where its render mode does not.</remarks>
public interface IComponentContextFactory
{
    /// <summary>Makes the context of a component of <paramref name="componentType"/> whose strings are read for the type <paramref name="localeSourceType"/> answers.</summary>
    /// <param name="componentType">The component's type, which its log is named for.</param>
    /// <param name="localeSourceType">The type whose locale files hold the component's strings, asked for as the component first reads one.</param>
    /// <returns>The context, which the component disposes.</returns>
    ComponentContext Create(Type componentType, Func<Type> localeSourceType);
}
