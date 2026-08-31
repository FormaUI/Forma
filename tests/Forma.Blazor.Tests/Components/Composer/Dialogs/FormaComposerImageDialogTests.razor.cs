namespace Forma.Blazor.Tests.Components.Composer.Dialogs;

public sealed partial class FormaComposerImageDialogTests
{
    public FormaComposerImageDialogTests()
    {
        Services.AddJsonLocalization();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
