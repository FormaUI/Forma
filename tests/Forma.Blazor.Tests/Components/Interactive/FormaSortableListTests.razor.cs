namespace Forma.Blazor.Tests.Components.Interactive;

public sealed partial class FormaSortableListTests
{
    public FormaSortableListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
