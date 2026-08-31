namespace Forma.Blazor.Tests.Components.Composer.RichTextEditor;

using Forma.Framework.Localization;

public sealed partial class FormaCompactRichTextEditorTests
{
    public FormaCompactRichTextEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
