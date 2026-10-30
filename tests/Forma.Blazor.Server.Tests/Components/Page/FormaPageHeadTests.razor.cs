namespace Forma.Blazor.Server.Tests.Components.Page;

public sealed partial class FormaPageHeadTests
{
    public FormaPageHeadTests()
    {
        // HeadOutlet reads the document's existing title through interop.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
