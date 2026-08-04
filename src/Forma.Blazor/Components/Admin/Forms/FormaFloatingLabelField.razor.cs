namespace Forma.Blazor.Components.Admin.Forms;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

public sealed partial class FormaFloatingLabelField : InputBase<string?>, IAsyncDisposable
{
    private const string ModulePath = "./_content/Forma.Blazor/Components/Admin/Forms/FormaFloatingLabelField.razor.js";

    private readonly string _inputId = $"fld-{Guid.NewGuid():N}";
    private ElementReference _inputRef;
    private JsModule _module = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The floating label text. Also serves as implicit placeholder.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Marks the field as required, showing a visual indicator and setting aria-required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>The HTML input type. Defaults to "text". Suitable values: text, email, tel, url, search.</summary>
    [Parameter] public string Type { get; set; } = "text";

    protected override void OnInitialized()
        => _module = new JsModule(JS, ModulePath);

    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(Label);

    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await _module.TryInvokeVoidAsync("initAutofillDetection", _inputRef);
    }

    public async ValueTask DisposeAsync()
        => await _module.DisposeAsync();
}
