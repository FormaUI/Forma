namespace Forma.UI.Blazor.Tests.Components.Interactive;

public sealed partial class FormaOverflowMenuTests
{
    public FormaOverflowMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
