namespace Forma.Blazor.Tests.Components.Windows;

public sealed partial class FormaRestAreaTests
{
    public FormaRestAreaTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
        JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
        JSInterop.SetupModule("./_content/forma/common/scripts/components/Window.js").SetupModule("attach", _ => true);
    }
}
