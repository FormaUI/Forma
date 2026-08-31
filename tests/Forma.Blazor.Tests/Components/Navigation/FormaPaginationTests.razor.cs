namespace Forma.Blazor.Tests.Components.Navigation;

public sealed partial class FormaPaginationTests
{
    public FormaPaginationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
