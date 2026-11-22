namespace Forma.Tests.Blazor;

using System.Globalization;
using System.Reflection;

using Bunit;
using Bunit.Rendering;

using Forma.Blazor;
using Forma.Framework.Culture;
using Forma.Framework.Emojis.Twemoji;
using Forma.Framework.Localization;
using Forma.Framework.Localization.Json;
using Forma.Tests.Culture;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

/// <summary>A bUnit context with the services every Forma component expects registered, and the input router's script answering the configuration every component's first render asks for, rendering as an interactive server circuit until a test sets another renderer.</summary>
public abstract class FormaBunitContext : BunitContext
{
    private const string InputRouterModule = "./_content/forma/input/scripts/InputRouter.js";

    private static readonly RendererInfo InteractiveServer = new("Server", isInteractive: true);

    private bool _isRendererInfoSet;

    // Read from this assembly, where the build records the referenced locale packs; a test's entry assembly is its runner's.
    /// <summary>The locale packs of the languages the library ships, which the context's localization reads.</summary>
    public static IReadOnlyList<Assembly> LocalePacks { get; } = LocaleAssemblyLoader.LoadPacks(typeof(FormaBunitContext).Assembly, NullLogger.Instance);

    /// <summary>The clock the rendered components read and wait on, standing at the moment the context was made, in the machine's time zone, until a test advances it.</summary>
    public FakeTimeProvider Clock { get; } = CreateClock();

    /// <summary>Creates a context carrying Forma's own service registrations.</summary>
    /// <remarks>
    /// The components read and wait on <see cref="Clock"/>, which moves only as a test advances it, so a wait a component starts, such as a
    /// data view's announcement of a slow load, never ends in the middle of a test and takes the renderer from it; a test registering a
    /// clock of its own after this one replaces it.
    /// </remarks>
    protected FormaBunitContext()
    {
        Services.AddJsonLocalization(new LocalizationOptions { LocalePacks = LocalePacks });
        Services.AddFormaTwemojiPack();
        Services.AddScoped<ICultureSwitch, TestCultureSwitch>();
        Services.AddFormaUIServices();
        Services.AddSingleton<TimeProvider>(Clock);

        // Every component's first render has the input router carry the host's routes to the page.
        JSInterop.SetupModule(InputRouterModule).SetupVoid("configure", _ => true).SetVoidResult();
    }

    /// <summary>Switches the cultures the rendered components follow through the test switch, leaving the caller's own cultures as they were.</summary>
    /// <param name="culture">The name of the culture that formats dates, numbers and collation.</param>
    /// <param name="uiCulture">The name of the culture that chooses strings and direction; <see langword="null"/> takes <paramref name="culture"/>.</param>
    /// <remarks>The switch is made inside this asynchronous method, so the cultures it applies to its flow are not the caller's.</remarks>
    public async Task ChangeCultureAsync(string culture, string? uiCulture = null)
        => await Services.GetRequiredService<ICultureSwitch>().SwitchAsync(
            CultureInfo.GetCultureInfo(culture),
            uiCulture is null ? null : CultureInfo.GetCultureInfo(uiCulture)).ConfigureAwait(false);

    /// <summary>Sets the renderer a test renders under.</summary>
    public new void SetRendererInfo(RendererInfo rendererInfo)
    {
        _isRendererInfoSet = true;
        base.SetRendererInfo(rendererInfo);
    }

    /// <inheritdoc />
    public override IRenderedComponent<TComponent> Render<TComponent>(RenderFragment renderFragment)
    {
        EnsureRendererInfo();

        return base.Render<TComponent>(renderFragment);
    }

    /// <inheritdoc />
    public override IRenderedComponent<TComponent> Render<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
    {
        EnsureRendererInfo();

        return base.Render(parameterBuilder);
    }

    /// <inheritdoc />
    public override IRenderedComponent<ContainerFragment> Render(RenderFragment renderFragment)
    {
        EnsureRendererInfo();

        return base.Render(renderFragment);
    }

    private static FakeTimeProvider CreateClock()
    {
        var clock = new FakeTimeProvider(TimeProvider.System.GetUtcNow());
        clock.SetLocalTimeZone(TimeZoneInfo.Local);

        return clock;
    }

    // Applied on the first render, which creates the renderer after every service a test registers.
    private void EnsureRendererInfo()
    {
        if (!_isRendererInfoSet)
        {
            SetRendererInfo(InteractiveServer);
        }
    }
}
