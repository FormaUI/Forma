namespace Forma.Blazor.Components.Inputs;

using Forma.Framework.Browser.Interop.Text;

using Microsoft.AspNetCore.Components.Forms;

/// <summary>A form's edit boundary: the edits of the Forma text fields bound in it, which an action settles before it reads the form.</summary>
public static class EditContextExtensions
{
    private static readonly object TextEditScopeKey = new();

    /// <summary>Waits until every Forma text field bound in the form holds every edit the reader made before the call.</summary>
    /// <returns>Whether every field holds them; a field that doesn't says so, and the action reading the form doesn't run.</returns>
    public static Task<bool> SettleEditsAsync(this EditContext editContext)
    {
        ArgumentNullException.ThrowIfNull(editContext);

        return editContext.Properties.TryGetValue(TextEditScopeKey, out var scope) ? ((TextEditScope)scope).SettleAsync() : Task.FromResult(true);
    }

    /// <summary>Submits the form once its fields' edits are settled: validates it and hands it to <paramref name="onValidSubmit"/> where it is valid, or to <paramref name="onInvalidSubmit"/>; a form whose edits couldn't be settled isn't submitted.</summary>
    /// <remarks>A form takes it as its submit, <c>OnSubmit="@(form => form.SubmitAsync(SaveAsync))"</c>, in place of its own validation, so Enter and a submit button wait as Forma's buttons do.</remarks>
    public static async Task SubmitAsync(this EditContext editContext, Func<EditContext, Task> onValidSubmit, Func<EditContext, Task>? onInvalidSubmit = null)
    {
        ArgumentNullException.ThrowIfNull(editContext);
        ArgumentNullException.ThrowIfNull(onValidSubmit);

        if (!await editContext.SettleEditsAsync())
        {
            return;
        }

        if (await editContext.ValidateAsync())
        {
            await onValidSubmit(editContext);
        }
        else if (onInvalidSubmit is not null)
        {
            await onInvalidSubmit(editContext);
        }
    }

    /// <summary>The edits of the text fields bound in the form.</summary>
    internal static TextEditScope GetTextEditScope(this EditContext editContext)
    {
        if (!editContext.Properties.TryGetValue(TextEditScopeKey, out var scope))
        {
            scope = new TextEditScope();
            editContext.Properties[TextEditScopeKey] = scope;
        }

        return (TextEditScope)scope;
    }
}
