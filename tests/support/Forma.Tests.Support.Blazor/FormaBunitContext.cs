namespace Forma.Tests.Blazor;

using System.Globalization;

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

/// <summary>A bUnit context with the services every Forma component expects registered, rendering as an interactive server circuit until a test sets another renderer.</summary>
public abstract class FormaBunitContext : BunitContext
{
    private static readonly RendererInfo InteractiveServer = new("Server", isInteractive: true);

    private bool _isRendererInfoSet;

    /// <summary>Creates a context carrying Forma's own service registrations.</summary>
    protected FormaBunitContext()
    {
        // Read from this assembly, where the build records the referenced locale packs; a test's entry assembly is its runner's.
        Services.AddJsonLocalization(new LocalizationOptions { LocalePacks = LocalePackLoader.Load(typeof(FormaBunitContext).Assembly, NullLogger.Instance) });
        Services.AddFormaTwemojiPack();
        Services.AddScoped<ICultureSwitch, TestCultureSwitch>();
        Services.AddFormaUI();
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

    // Applied on the first render, which creates the renderer after every service a test registers.
    private void EnsureRendererInfo()
    {
        if (!_isRendererInfoSet)
        {
            SetRendererInfo(InteractiveServer);
        }
    }
}
