namespace Forma.Blazor.Tests.Components.Windows;

public sealed partial class FormaWindowTests
{
    public FormaWindowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Router = new InputRouterProbe(this);
        JSInterop.SetupModule("./_content/forma/browser/scripts/components/Window.js").SetupModule("attach", _ => true);
        JSInterop.SetupModule("./_content/forma/browser/scripts/components/Frame.js").SetupModule("attach", _ => true);
    }

    /// <summary>The input router's stand-in, recording every window's registration from the first render and delivering dismissals to a window.</summary>
    internal InputRouterProbe Router { get; }
}
