namespace Forma.Blazor.BUnit.Tests.Components.Windows;

public sealed partial class FormaRestAreaTests
{
    public FormaRestAreaTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
        JSInterop.SetupModule("./_content/forma/browser/scripts/components/Window.js").SetupModule("attach", _ => true);
    }
}
