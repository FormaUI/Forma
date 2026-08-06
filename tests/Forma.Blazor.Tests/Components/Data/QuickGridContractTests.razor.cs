namespace Forma.Blazor.Tests.Components.Data;

public sealed partial class QuickGridContractTests
{
    public QuickGridContractTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
