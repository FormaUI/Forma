namespace Forma.Blazor.Tests.Components.Bars;

public sealed partial class FormaCommandBarTests
{
    public FormaCommandBarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
