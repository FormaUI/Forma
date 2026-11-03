namespace Forma.Blazor.BUnit.Tests.Components.Menus;

using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

public sealed partial class FormaShareMenuTests
{
    public FormaShareMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFormaShareNetworks();
    }
}
