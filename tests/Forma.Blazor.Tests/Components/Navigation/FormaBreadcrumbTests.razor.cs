namespace Forma.Blazor.Tests.Components.Navigation;

public sealed partial class FormaBreadcrumbTests
{
    public FormaBreadcrumbTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
