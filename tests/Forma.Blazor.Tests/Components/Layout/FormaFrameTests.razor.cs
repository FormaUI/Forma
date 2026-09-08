namespace Forma.Blazor.Tests.Components.Layout;

public sealed partial class FormaFrameTests
{
    public FormaFrameTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
        JSInterop.SetupModule("./_content/forma/common/scripts/components/Frame.js").SetupModule("attach", _ => true);
    }
}
