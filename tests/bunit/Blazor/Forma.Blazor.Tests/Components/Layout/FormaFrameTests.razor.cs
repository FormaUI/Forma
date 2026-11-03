namespace Forma.Blazor.Tests.Components.Layout;

public sealed partial class FormaFrameTests
{
    public FormaFrameTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/forma/browser/scripts/components/Frame.js").SetupModule("attach", _ => true);
    }
}
