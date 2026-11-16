namespace Forma.Tests.Culture;

using Forma.Framework.Culture;

/// <summary>A user scope's culture initializer that counts how often it was asked to initialize and runs what a test gives it on each.</summary>
public sealed class FakeCultureScopeInitializer : ICultureScopeInitializer
{
    /// <summary>How often the initializer was asked to initialize.</summary>
    public int InitializeCount { get; private set; }

    /// <summary>What each initialization runs, as a render mode's own initializer registers the scope.</summary>
    public Action? OnInitialize { get; init; }

    /// <inheritdoc />
    public void Initialize()
    {
        InitializeCount++;
        OnInitialize?.Invoke();
    }
}
