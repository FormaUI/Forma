namespace Forma.Blazor.Tests.Components.Admin.Data;

public sealed partial class QuickGridContractTests
{
    public QuickGridContractTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
