namespace Forma.UI.Blazor.Tests.Components.Composer.Dialogs;

public sealed partial class FormaComposerLinkDialogTests
{
    public FormaComposerLinkDialogTests()
    {
        Services.AddJsonLocalization();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
