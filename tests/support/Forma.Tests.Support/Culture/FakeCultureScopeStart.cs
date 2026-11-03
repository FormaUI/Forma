namespace Forma.Tests.Culture;

using Forma.Framework.Culture;

/// <summary>A user scope's part in the culture that counts how often it was started and runs what a test gives it on each start.</summary>
public sealed class FakeCultureScopeStart : ICultureScopeStart
{
    /// <summary>How often the scope's part was started.</summary>
    public int Starts { get; private set; }

    /// <summary>What each start runs, as a render mode's own start registers the scope.</summary>
    public Action? OnStart { get; init; }

    /// <inheritdoc />
    public void Start()
    {
        Starts++;
        OnStart?.Invoke();
    }
}
