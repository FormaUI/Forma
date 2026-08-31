namespace Forma.Blazor.Tests.Components.Feedback;

using Forma.Blazor.Components.Feedback;

public sealed partial class FormaStatusBadgeTests
{
    public FormaStatusBadgeTests()
    {
        Services.AddSingleton<IStatusBadgeCatalog, StatusBadgeCatalog>();
    }
}
