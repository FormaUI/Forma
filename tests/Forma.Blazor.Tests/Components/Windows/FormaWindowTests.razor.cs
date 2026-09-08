namespace Forma.Blazor.Tests.Components.Windows;

public sealed partial class FormaWindowTests
{
    public FormaWindowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
        JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
        JSInterop.SetupModule("./_content/forma/common/scripts/components/Window.js").SetupModule("attach", _ => true);
        JSInterop.SetupModule("./_content/forma/common/scripts/components/Frame.js").SetupModule("attach", _ => true);
    }
}
