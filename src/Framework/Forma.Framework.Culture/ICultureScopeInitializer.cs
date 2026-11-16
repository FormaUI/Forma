namespace Forma.Framework.Culture;

/// <summary>Initializes culture support for a user scope when the render mode does not initialize it.</summary>
public interface ICultureScopeInitializer
{
    /// <summary>Initializes culture support in the calling component's execution flow. Does nothing if the scope is already initialized.</summary>
    void Initialize();
}
