namespace Forma.Tests.Blazor;

using Bunit;
using Bunit.Rendering;

using Forma.Blazor;
using Forma.Framework.Emojis.Twemoji;
using Forma.Framework.Localization;
using Forma.Framework.Localization.Json;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>A bUnit context with the services every Forma component expects already registered, rendering as an interactive server circuit unless a test says otherwise.</summary>
public abstract class FormaBunitContext : BunitContext
{
    private static readonly RendererInfo InteractiveServer = new("Server", isInteractive: true);

    private bool _isRendererInfoSet;

    /// <summary>Creates a context carrying Forma's own service registrations.</summary>
    protected FormaBunitContext()
    {
        // The locale packs this project references are recorded in its own assembly by the build, as an
        // application's are in its; a test's entry assembly is its runner's, so the packs are read from here.
        Services.AddJsonLocalization(new LocalizationOptions { LocalePacks = LocalePackLoader.Load(typeof(FormaBunitContext).Assembly, NullLogger.Instance) });
        Services.AddFormaTwemojiPack();
        Services.AddFormaUI();
    }

    /// <summary>Sets the renderer a test renders under, in place of the interactive server default.</summary>
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

    // The renderer is created on the first render, after every service a test registers, so the default is applied there.
    private void EnsureRendererInfo()
    {
        if (!_isRendererInfoSet)
        {
            SetRendererInfo(InteractiveServer);
        }
    }
}
