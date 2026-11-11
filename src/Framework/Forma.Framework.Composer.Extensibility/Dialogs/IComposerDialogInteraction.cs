namespace Forma.Framework.Composer.Dialogs;

/// <summary>What the body of a composer dialog invokes on the dialog it fills.</summary>
public interface IComposerDialogInteraction
{
    /// <summary>Confirms the dialog as its confirming action does, putting what the body answers into the document and closing the dialog, or keeping it open with the reasons it was refused.</summary>
    /// <remarks>A confirmation asked for while one is under way is that one, so the body's own request and the confirming action insert once between them.</remarks>
    Task ConfirmAsync();
}
