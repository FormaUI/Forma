namespace Forma.UI.Blazor.Tests.Components.Bars;

public sealed partial class FormaSearchToolbarTests
{
    public FormaSearchToolbarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
