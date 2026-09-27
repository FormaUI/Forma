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

    /// <summary>The input router's stand-in, set up before any render so every window's registration is recorded and a dismissal can be delivered to it.</summary>
    internal InputRouterProbe Router { get; }
}
