namespace Forma.Blazor.Tests.Components.Menus;

using Forma.Framework.Localization;
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

public sealed partial class FormaShareMenuTests
{
    public FormaShareMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
        Services.AddFormaShareNetworks();
    }
}
