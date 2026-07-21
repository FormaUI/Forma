using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Site.UI.Components.Admin.Forms;

public sealed partial class SiteFloatingLabelField : InputBase<string?>, IAsyncDisposable
{
    private readonly string _inputId = $"fld-{Guid.NewGuid():N}";
    private ElementReference _inputRef;
    private IJSObjectReference? _module;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The floating label text. Also serves as implicit placeholder.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Marks the field as required, showing a visual indicator and setting aria-required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>The HTML input type. Defaults to "text". Suitable values: text, email, tel, url, search.</summary>
    [Parameter] public string Type { get; set; } = "text";

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
    }

    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Site.UI/Components/Admin/Forms/SiteFloatingLabelField.razor.js").ConfigureAwait(false);
            await _module.InvokeVoidAsync("initAutofillDetection", _inputRef).ConfigureAwait(false);
        }
        catch (JSException)
        {
            // Circuit disconnected before interop completed — safe to ignore.
        }
    }

    public async ValueTask DisposeAsync()
    {
        var module = _module;
        _module = null;
        if (module is not null)
        {
            try
            {
                await module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected — safe to ignore.
            }
        }
    }
}
