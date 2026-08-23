namespace Forma.UI.Blazor.Tests.Components.Menus;

public sealed partial class FormaDropDownMenuTests
{
    public FormaDropDownMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
