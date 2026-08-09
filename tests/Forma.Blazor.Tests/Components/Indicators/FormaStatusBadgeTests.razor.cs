namespace Forma.Blazor.Tests.Components.Indicators;

using Forma.Blazor.Components.Indicators;

public sealed partial class FormaStatusBadgeTests
{
    public FormaStatusBadgeTests()
    {
        Services.AddSingleton<IStatusBadgeCatalog, StatusBadgeCatalog>();
    }
}
