namespace Forma.Blazor.Tests.Components.Input;

public sealed partial class FormaMultiSelectDropDownTests
{
    public FormaMultiSelectDropDownTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
