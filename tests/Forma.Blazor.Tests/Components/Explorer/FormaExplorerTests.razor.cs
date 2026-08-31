namespace Forma.Blazor.Tests.Components.Explorer;

using Forma.Framework.Localization;

public sealed partial class FormaExplorerTests
{
    public FormaExplorerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
