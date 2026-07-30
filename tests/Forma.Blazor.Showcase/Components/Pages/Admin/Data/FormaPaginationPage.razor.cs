using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Forma.Blazor.Showcase.Components.Pages.Admin.Data;

public sealed partial class FormaPaginationPage : ComponentBase
{
    [Inject] private IStringLocalizer<FormaPaginationPage> L { get; set; } = default!;

    private int _fewPage = 1;
    private int _manyPage = 10;
    private int _jumpPage = 1;
    private int _inputOnlyPage = 1;
    private int _barePage = 1;
    private int _formPage = 1;
    private int _formSubmits;

    private void HandleFewPageChanged(int page)
        => _fewPage = page;

    private void HandleManyPageChanged(int page)
        => _manyPage = page;

    private void HandleJumpPageChanged(int page)
        => _jumpPage = page;

    private void HandleInputOnlyPageChanged(int page)
        => _inputOnlyPage = page;

    private void HandleBarePageChanged(int page)
        => _barePage = page;

    private void HandleFormPageChanged(int page)
        => _formPage = page;

    private void HandleFormSubmit()
        => _formSubmits++;
}
