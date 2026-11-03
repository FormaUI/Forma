namespace Forma.Blazor.Server.BUnit.Tests.Components.Page;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.Extensions.DependencyInjection;

public sealed partial class FormaPageHeadTests
{
    public FormaPageHeadTests()
    {
        // HeadOutlet reads the document's existing title through interop.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(CultureSwitchPolicy.Default);
    }
}
