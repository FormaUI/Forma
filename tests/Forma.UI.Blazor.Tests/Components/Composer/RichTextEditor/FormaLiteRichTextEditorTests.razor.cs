namespace Forma.UI.Blazor.Tests.Components.Composer.RichTextEditor;

using Forma.Framework.Localization;

public sealed partial class FormaLiteRichTextEditorTests
{
    public FormaLiteRichTextEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
