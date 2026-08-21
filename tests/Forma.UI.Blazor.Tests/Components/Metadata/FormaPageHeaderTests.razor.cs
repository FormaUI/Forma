namespace Forma.UI.Blazor.Tests.Components.Metadata;

public sealed partial class FormaPageHeaderTests
{
    public FormaPageHeaderTests()
    {
        // HeadOutlet reads the document's existing title through interop.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
