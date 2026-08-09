namespace Forma.UI.Blazor.Tests.Components.Data;

public sealed partial class FormaDataGridTests
{
    public FormaDataGridTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
