namespace Forma.Blazor.Tests.Components.Admin.Data;

public sealed partial class FormaPaginationTests
{
    public FormaPaginationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
