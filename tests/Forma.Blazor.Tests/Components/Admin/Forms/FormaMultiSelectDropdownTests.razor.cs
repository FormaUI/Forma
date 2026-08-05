namespace Forma.Blazor.Tests.Components.Admin.Forms;

public sealed partial class FormaMultiSelectDropdownTests
{
    public FormaMultiSelectDropdownTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
