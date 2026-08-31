namespace Forma.Blazor.Tests.Components.Collections;

public sealed partial class QuickGridContractTests
{
    public QuickGridContractTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
