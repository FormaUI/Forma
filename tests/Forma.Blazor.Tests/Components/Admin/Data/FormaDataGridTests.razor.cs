namespace Forma.Blazor.Tests.Components.Admin.Data;

public sealed partial class FormaDataGridTests
{
    public FormaDataGridTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
