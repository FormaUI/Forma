namespace Forma.UI.Blazor.Showcase.Components.Pages.Data;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Components.Data;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaPaginationPage : ComponentBase
{
    [Inject] private IStringLocale<FormaPaginationPage> L { get; set; } = default!;

    private static readonly FormaPaginationOptions NavigationStepsOnly =
        new() { EnableNavigationSteps = true, EnableNavigationInput = false };

    private static readonly FormaPaginationOptions NavigationBar =
        new() { EnableNavigationSteps = true };

    private static readonly FormaPaginationOptions NavigationInput =
        new() { EnableNavigationSteps = false };

    private static readonly FormaPaginationOptions NavigationInputOnly =
        new() { EnableGoButton = false };

    private static readonly IReadOnlyList<GridItem> _gridItems = Enumerable.Range(1, 47)
        .Select(id => new GridItem(id, $"Sample item {id}"))
        .ToArray();

    private const int GridPageSize = 10;

    private int _fewPage = 1;
    private int _manyPage = 10;
    private int _gridPage = 1;
    private int _jumpPage = 1;
    private int _inputOnlyPage = 1;
    private int _barePage = 1;
    private int _formPage = 1;
    private int _formSubmits;

    private void HandleFewPageChanged(int page)
        => _fewPage = page;

    private void HandleManyPageChanged(int page)
        => _manyPage = page;

    private void HandleGridPageChanged(int page)
        => _gridPage = page;

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

    private IQueryable<GridItem> GridPageItems => _gridItems
        .Skip((_gridPage - 1) * GridPageSize)
        .Take(GridPageSize)
        .AsQueryable();

    private sealed record GridItem(int Id, string Title);
}
