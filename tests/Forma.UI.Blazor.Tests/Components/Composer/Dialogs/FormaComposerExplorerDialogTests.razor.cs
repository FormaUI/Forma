namespace Forma.UI.Blazor.Tests.Components.Composer.Dialogs;

public sealed partial class FormaComposerExplorerDialogTests
{
    public FormaComposerExplorerDialogTests()
    {
        Services.AddJsonLocalization();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
