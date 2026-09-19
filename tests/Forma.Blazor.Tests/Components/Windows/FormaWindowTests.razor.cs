namespace Forma.Blazor.Tests.Components.Windows;

public sealed partial class FormaWindowTests
{
    public FormaWindowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
        JSInterop.SetupModule("./_content/forma/browser/scripts/components/Window.js").SetupModule("attach", _ => true);
        JSInterop.SetupModule("./_content/forma/browser/scripts/components/Frame.js").SetupModule("attach", _ => true);
    }
}
