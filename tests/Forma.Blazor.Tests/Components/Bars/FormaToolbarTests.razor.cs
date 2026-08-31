namespace Forma.Blazor.Tests.Components.Bars;

public sealed partial class FormaToolbarTests
{
    public FormaToolbarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
