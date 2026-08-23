namespace Forma.UI.Blazor.Tests.Components.Collections;

public sealed partial class FormaDataGridTests
{
    public FormaDataGridTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
