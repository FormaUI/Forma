namespace Forma.Blazor.Tests.Components.Layout;

public sealed partial class FormaFreezePaneTests
{
    public FormaFreezePaneTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
