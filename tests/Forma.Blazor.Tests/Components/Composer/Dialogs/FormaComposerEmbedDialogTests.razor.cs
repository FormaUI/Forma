namespace Forma.Blazor.Tests.Components.Composer.Dialogs;

public sealed partial class FormaComposerEmbedDialogTests
{
    public FormaComposerEmbedDialogTests()
    {
        Services.AddJsonLocalization();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
