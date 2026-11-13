namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A checkbox bound to a Boolean, with an indeterminate state and a checked state the caller can take over.</summary>
public sealed partial class FormaCheckBox : FormaInputBase<bool>
{
    /// <summary>Whether the checkbox shows the "some but not all" state.</summary>
    /// <remarks>The state has no content attribute, so it is set on the element through script after render; a checkbox that never becomes indeterminate never calls the script.</remarks>
    [Parameter] public bool Indeterminate { get; set; }

    /// <summary>Whether the checkbox is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the checkbox is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Whether the native toggle is suppressed, leaving the checked state to the caller.</summary>
    [Parameter] public bool PreventDefault { get; set; }

    /// <summary>The label rendered next to the checkbox, wrapped together with it in a label element.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private CheckBox _checkBox = default!;

    // Starts at the element's own default, so a checkbox never made indeterminate never calls the script.
    private bool _isIndeterminateApplied;

    private bool IsDisabled
        => Disabled || (OnClick.HasDelegate && !RendererInfo.IsInteractive);

    /// <inheritdoc />
    protected override bool HasFieldContainer
        => false;

    /// <inheritdoc />
    protected override bool CanFocus
        => !Disabled && base.CanFocus;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _checkBox = new CheckBox(Context.JS);

    private void HandleChange(ChangeEventArgs args)
        => CurrentValue = args.Value is true;

    // The browser undoes a prevented click's toggle once the click ends, so the caller is told after it: a state drawn inside the click,
    // as WebAssembly draws it, would be undone with the toggle while the component still holds it.
    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (PreventDefault)
        {
            await Task.Yield();
        }

        await OnClick.InvokeAsync(args);
    }

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
        result = bool.TryParse(value, out var parsedValue) && parsedValue;
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
