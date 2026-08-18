namespace Forma.UI.Blazor.Tests.Components.Indicators;

using Forma.UI.Blazor.Components.Indicators;

public sealed partial class FormaStatusBadgeTests
{
    public FormaStatusBadgeTests()
    {
        Services.AddSingleton<IStatusBadgeCatalog, StatusBadgeCatalog>();
    }
}
