namespace Forma.UI.Blazor.Tests.Components.Menus;

using Forma.Framework.Localization;
using Forma.UI.Features.Share;

public sealed partial class FormaShareMenuTests
{
    public FormaShareMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
        Services.AddFormaShareNetworks();
    }
}
