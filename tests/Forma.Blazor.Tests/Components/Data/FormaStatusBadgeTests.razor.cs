namespace Forma.Blazor.Tests.Components.Data;

using Forma.Blazor.Abstractions.Components.Data;
using Forma.Blazor.Core.Components.Data;

public sealed partial class FormaStatusBadgeTests
{
    public FormaStatusBadgeTests()
    {
        Services.AddSingleton<IStatusBadgeCatalog, StatusBadgeCatalog>();
    }
}
