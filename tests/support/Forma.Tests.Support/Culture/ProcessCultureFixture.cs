namespace Forma.Tests.Culture;

using System.Globalization;

using Forma.Tests.Shared;

/// <summary>Holds the test's flow in English and restores the process's default cultures, which an application sets, on disposal.</summary>
public sealed class ProcessCultureFixture : IDisposable
{
    private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _defaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
    private readonly CultureFixture _flowCulture = new("en-US");

    /// <inheritdoc />
    public void Dispose()
    {
        CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _defaultUICulture;
        _flowCulture.Dispose();
    }
}
