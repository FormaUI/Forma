namespace Forma.Blazor.Components.Admin.Forms;

using Forma.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A styled checkbox with a script-driven indeterminate state, optionally leaving the checked state to the caller.</summary>
public sealed partial class FormaCheckbox : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/Forma.Blazor/Components/Admin/Forms/FormaCheckbox.razor.js";

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
    private JsModule _module = default!;

    // Seeded to the element's own default, so a checkbox that is never indeterminate
    // — every row and option checkbox — never imports the module or calls into it.
    private bool _indeterminateApplied;

    // Host class is merged rather than splatted, so supplying one never drops the checkbox's own styling.
    private string CssClass
    {
        get
        {
            var hostClass = AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("class", out var value)
                ? value?.ToString()
                : null;

            return string.IsNullOrWhiteSpace(hostClass) ? "forma-checkbox" : $"forma-checkbox {hostClass}";
        }
    }

    protected override void OnInitialized()
        => _module = new JsModule(JS, ModulePath);

    private Task HandleChangeAsync(ChangeEventArgs args)
        => CheckedChanged.InvokeAsync(args.Value is true);

    private Task HandleClickAsync(MouseEventArgs args)
        => OnClick.InvokeAsync(args);

    // "Some but not all" is an IDL-only property with no content attribute, so it can
    // only be set from script, and only once the element exists to set it on.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_indeterminateApplied == Indeterminate)
        {
            return;
        }

        if (await _module.TryInvokeVoidAsync("setIndeterminate", _element, Indeterminate))
        {
            _indeterminateApplied = Indeterminate;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _module.DisposeAsync();
}
