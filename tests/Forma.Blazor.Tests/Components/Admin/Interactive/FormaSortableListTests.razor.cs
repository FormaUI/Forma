namespace Forma.Blazor.Tests.Components.Admin.Interactive;

public sealed partial class FormaSortableListTests
{
    public FormaSortableListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
