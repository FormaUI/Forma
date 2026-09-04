namespace Forma.Blazor.Components.Inputs;

using Forma.Framework.Interop.Operations;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A styled checkbox with a script-driven indeterminate state, optionally leaving the checked state to the caller.</summary>
public sealed partial class FormaCheckBox : ComponentBase, IAsyncDisposable
{
    /// <summary>Whether the checkbox is checked.</summary>
    [Parameter] public bool Checked { get; set; }

    /// <summary>Raised with the new state after a normal toggle.</summary>
    [Parameter] public EventCallback<bool> CheckedChanged { get; set; }

    /// <summary>Shows the "some but not all" visual. Set from script, since it has no content attribute.</summary>
    [Parameter] public bool Indeterminate { get; set; }

    /// <summary>Blocks interaction and dims the checkbox.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the checkbox is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Suppresses the native toggle, leaving the checked state entirely to the caller.</summary>
    [Parameter] public bool PreventDefault { get; set; }

    /// <summary>Label rendered next to the checkbox. Wraps both in a label element, so clicking the text toggles the checkbox too.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional attributes applied to the input element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _element;
    private CheckBox _checkBox = default!;

    // Seeded to the element's own default, so a checkbox that is never indeterminate
    // — every row and option checkbox — never imports the module or calls into it.
    private bool _isIndeterminateApplied;

    // Host class is merged rather than splatted, so supplying one never drops the checkbox's own styling.
    private string CssClass
        => AdditionalAttributes.MergeClass("forma-checkbox");

    /// <inheritdoc />
    protected override void OnInitialized()
        => _checkBox = new CheckBox(JS);

    private Task HandleChangeAsync(ChangeEventArgs args)
        => CheckedChanged.InvokeAsync(args.Value is true);

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

        if (await _checkBox.SetIndeterminateAsync(_element, Indeterminate))
        {
            _isIndeterminateApplied = Indeterminate;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _checkBox.DisposeAsync();
}
