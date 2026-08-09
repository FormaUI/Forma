namespace Forma.UI.Blazor.Tests.Components.Interactive;

public sealed partial class FormaDropDownMenuTests
{
    public FormaDropDownMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
