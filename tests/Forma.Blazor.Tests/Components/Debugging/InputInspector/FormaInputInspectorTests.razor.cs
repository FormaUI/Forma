namespace Forma.Blazor.Tests.Components.Debugging.InputInspector;

public sealed partial class FormaInputInspectorTests
{
    public FormaInputInspectorTests()
    {
        Services.AddJsonLocalization();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
