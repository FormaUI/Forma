namespace Forma.Framework.Culture;

/// <summary>A user scope's part in the culture, started as the scope's first component initializes, for a render mode that starts nothing of its own, as a web view's starts nothing.</summary>
public interface ICultureScopeStart
{
    /// <summary>Starts the scope's part in the culture, from the flow of the component initializing; a scope already started is left as it is.</summary>
    void Start();
}
