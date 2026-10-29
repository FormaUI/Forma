namespace Forma.Blazor.Showcase.Components.Pages.Navigation;

using Forma.Blazor.Components.Navigation;

public sealed partial class FormaPaginationPage
{

    private static readonly PaginationOptions InputByOption =
        new() { NavigationInput = true };

    private static readonly PaginationOptions WithoutGoButton =
        new() { NavigationInput = true, GoButton = false };

    private static readonly PaginationOptions WithWheel =
        new() { PageOnWheelGesture = true };

    private static readonly IQueryable<GridItem> _gridItems = Enumerable.Range(1, 47)
        .Select(id => new GridItem(id, $"Sample item {id}"))
        .AsQueryable();

    private int _fewPage = 1;
    private int _manyPage = 5;
    private int _inputPage = 1;
    private int _noGoPage = 1;
    private int _wheelPage = 1;
    private int _formPage = 1;
    private int _formSubmits;

    private void HandleFewPageChanged(int page)
        => _fewPage = page;

    private void HandleManyPageChanged(int page)
        => _manyPage = page;

    private void HandleInputPageChanged(int page)
        => _inputPage = page;

    private void HandleNoGoPageChanged(int page)
        => _noGoPage = page;

    private void HandleWheelPageChanged(int page)
        => _wheelPage = page;

    private void HandleFormPageChanged(int page)
        => _formPage = page;

    private void HandleFormSubmit()
        => _formSubmits++;

    private sealed record GridItem(int Id, string Title);
}
