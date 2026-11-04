namespace Forma.Blazor.Components.Inputs;

using Forma.Framework.Browser.Interop.Text;

using Microsoft.AspNetCore.Components.Forms;

/// <summary>A form's edit boundary: the edits of the Forma text fields bound in it, which the form's validation, and an action reading the form, settle before they read it.</summary>
/// <remarks>
/// The form's fields join one scope on its <see cref="EditContext"/>, which takes part in every asynchronous validation of the form, as
/// <c>EditForm</c>'s submit and <see cref="EditContext.ValidateAsync(CancellationToken)"/> run it: the validation waits until every field
/// holds the reader's edits, and fails where a field's text can't follow them. A synchronous validation reads the form as it stands.
/// </remarks>
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

    /// <summary>The edits of the text fields bound in the form.</summary>
    internal static TextEditScope GetTextEditScope(this EditContext editContext)
    {
        if (editContext.Properties.TryGetValue(TextEditScopeKey, out var existing))
        {
            return (TextEditScope)existing;
        }

        var scope = new TextEditScope();
        editContext.Properties[TextEditScopeKey] = scope;
        editContext.OnValidationRequested += (_, args) =>
        {
            // A synchronous validation hands its handlers the shared arguments, which take no asynchronous work.
            if (!ReferenceEquals(args, ValidationRequestedEventArgs.Empty))
            {
                args.AddAsyncValidator(_ => SettleForValidationAsync(scope));
            }
        };

        return scope;
    }

    // A validation pass contains its validators' failures and fails with them, which is how a text that can't follow fails the form.
    private static async Task SettleForValidationAsync(TextEditScope scope)
    {
        if (!await scope.SettleAsync())
        {
            throw new InvalidOperationException("A Forma field's text didn't arrive in full.");
        }
    }
}
