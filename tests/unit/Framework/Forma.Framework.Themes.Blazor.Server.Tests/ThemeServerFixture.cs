namespace Forma.Framework.Themes.Blazor.Server.Tests;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Requests to a server registering Forma's theme and Harbor, with the store, the record and the given choice each check sets.</summary>
internal sealed class ThemeServerFixture : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly List<IServiceScope> _scopes = [];

    public ThemeServerFixture(FakeThemePreferenceStore? store = null, Func<HttpContext, ThemeChoice?>? givenChoice = null)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddFormaThemeServerIntegration(givenChoice);
        services.AddFormaThemes(themes =>
        {
            themes.Add(Define("forma"));
            themes.Add(Define("harbor"));
        });

        if (store is not null)
        {
            services.TryAddSingleton<IThemePreferenceStore>(store);
        }

        _provider = services.BuildServiceProvider();
    }

    public static ThemeDefinition Define(string name)
        => new(name, LocalizableText.For<ThemeServerFixture>(name), ColorSchemes.Light | ColorSchemes.Dark, $"css/{name}.css");

    /// <summary>A request carrying <paramref name="record"/> as its choice cookie, over HTTPS where <paramref name="isHttps"/>.</summary>
    public DefaultHttpContext CreateContext(string? record = null, bool isHttps = false)
    {
        var scope = _provider.CreateScope();
        _scopes.Add(scope);
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Scheme = isHttps ? "https" : "http";
        context.Request.Host = new HostString("example.test");
        context.Response.Body = new MemoryStream();

        if (record is not null)
        {
            context.Request.Headers.Cookie = $"{(isHttps ? "__Host-forma-theme" : "forma-theme")}={record}";
        }

        return context;
    }

    public void Dispose()
    {
        _scopes.ForEach(scope => scope.Dispose());
        _provider.Dispose();
    }
}

/// <summary>A store holding the preference a check gives it, which can fail to read or to write.</summary>
internal sealed class FakeThemePreferenceStore : IThemePreferenceStore
{
    public ThemePreference? Preference { get; set; }

    public bool FailsToRead { get; init; }

    public bool FailsToWrite { get; init; }

    public int Reads { get; private set; }

    public List<ThemePreference> Writes { get; } = [];

    public ValueTask<ThemePreference?> ReadAsync(CancellationToken cancellationToken)
    {
        Reads++;

        return FailsToRead ? throw new InvalidOperationException("The store is down.") : ValueTask.FromResult(Preference);
    }

    public ValueTask WriteAsync(ThemePreference preference, CancellationToken cancellationToken)
    {
        if (FailsToWrite)
        {
            throw new InvalidOperationException("The store is down.");
        }

        Writes.Add(preference);
        Preference = preference;

        return ValueTask.CompletedTask;
    }
}
