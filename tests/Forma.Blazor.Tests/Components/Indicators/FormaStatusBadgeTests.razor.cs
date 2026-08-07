namespace Forma.Blazor.Tests.Components.Indicators;

using Forma.Blazor.Abstractions.Components.Indicators;
using Forma.Blazor.Core.Components.Indicators;

public sealed partial class FormaStatusBadgeTests
{
    public FormaStatusBadgeTests()
    {
        Services.AddSingleton<IStatusBadgeCatalog, StatusBadgeCatalog>();
    }
}
