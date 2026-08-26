namespace Forma.UI.Blazor.Tests.Components.Feedback;

using Forma.UI.Blazor.Components.Feedback;

public sealed partial class FormaStatusBadgeTests
{
    public FormaStatusBadgeTests()
    {
        Services.AddSingleton<IStatusBadgeCatalog, StatusBadgeCatalog>();
    }
}
