namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Common.Interop.Operations;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A styled checkbox bound to a Boolean, with an indeterminate state, optionally leaving the checked state to the caller.</summary>
public sealed partial class FormaCheckBox : FormaInputBase<bool>
{
    /// <summary>Shows the "some but not all" visual.</summary>
    /// <remarks>The state has no content attribute, so it is set on the element through script, and only once a checkbox becomes indeterminate.</remarks>
    [Parameter] public bool Indeterminate { get; set; }

    /// <summary>Blocks interaction and dims the checkbox.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the checkbox is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Suppresses the native toggle, leaving the checked state entirely to the caller.</summary>
    [Parameter] public bool PreventDefault { get; set; }

    /// <summary>Label rendered next to the checkbox. Wraps both in a label element, so clicking the text toggles the checkbox too.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private CheckBox _checkBox = default!;

    // Seeded to the element's own default, so a checkbox that is never indeterminate
    // — every row and option checkbox — never imports the module or calls into it.
    private bool _isIndeterminateApplied;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _checkBox = new CheckBox(Context.JS);

    private void HandleChange(ChangeEventArgs args)
        => CurrentValue = args.Value is true;

    private Task HandleClickAsync(MouseEventArgs args)
        => OnClick.InvokeAsync(args);

    // "Some but not all" is an IDL-only property with no content attribute, so it can
    // only be set from script, and only once the element exists to set it on.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isIndeterminateApplied == Indeterminate)
        {
            return;
        }

        if (await _checkBox.SetIndeterminateAsync(RootElement, Indeterminate))
        {
            _isIndeterminateApplied = Indeterminate;
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-checkbox");

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out bool result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = bool.TryParse(value, out var parsed) && parsed;
        validationErrorMessage = null;

        return true;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _checkBox.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
