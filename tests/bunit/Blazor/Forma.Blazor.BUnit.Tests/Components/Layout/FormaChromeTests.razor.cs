namespace Forma.Blazor.BUnit.Tests.Components.Layout;

public sealed partial class FormaChromeTests
{
    public FormaChromeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/forma/browser/scripts/components/Frame.js").SetupModule("attach", _ => true);
    }
}
