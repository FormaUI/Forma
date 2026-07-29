using Forma.Blazor.Abstractions.Components.Data;
using Forma.Blazor.Core.Components.Data;

namespace Forma.Blazor.Tests.Components.Admin.Data;

public sealed partial class FormaStatusBadgeTests
{
    public FormaStatusBadgeTests()
    {
        Services.AddSingleton<IStatusBadgeCatalog, StatusBadgeCatalog>();
    }
}
