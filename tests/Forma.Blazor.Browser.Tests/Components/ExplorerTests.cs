namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The explorer's main interaction, in every runtime.</summary>
public sealed class ExplorerTests
{
    public sealed class Explorer(BrowserHost host)
    {
        private static readonly string[] AllFileNames = ["cover.txt", "lap-times.txt"];

        private static readonly string[] RacesFileNames = ["lap-times.txt"];

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_list_every_item_and_only_the_open_folder_s_once_one_is_opened(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "explorer");
            await using var _ = context;
            var names = page.Locator("#check .forma-explorer-item-name");
            var sidebar = page.Locator("#check .forma-explorer-sidebar");
            var races = sidebar.Locator("[role=treeitem]", new() { HasText = "Races" });
            await Assertions.Expect(names).ToHaveTextAsync(AllFileNames);
            await Assertions.Expect(races).ToHaveAttributeAsync("aria-selected", "false");

            // Act
            await sidebar.GetByRole(AriaRole.Button, new() { Name = "Races", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(names).ToHaveTextAsync(RacesFileNames);
            await Assertions.Expect(races).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(sidebar.Locator("[role=treeitem]", new() { HasText = "All Media" })).ToHaveAttributeAsync("aria-selected", "false");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Races");
        }
    }
}
