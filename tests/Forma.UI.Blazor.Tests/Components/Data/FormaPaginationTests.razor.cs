namespace Forma.UI.Blazor.Tests.Components.Data;

public sealed partial class FormaPaginationTests
{
    public FormaPaginationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
