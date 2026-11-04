namespace Forma.Blazor.Browser.Tests.Components;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>Values a reader can make larger than a circuit message, through each channel they take to .NET, and the actions that read them, in every runtime under Blazor's default hub limit.</summary>
public sealed class TransferTests
{
    // A value as the check page writes it: its length and the start of its SHA-256.
    private static string Describe(string value)
        => $"{value.Length}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16]}";

    public sealed class Forms(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_save_a_text_of_megabytes_through_a_form_of_forma_own(string runtime)
        {
            // Arrange — two megabytes typed into the text area as the page turns interactive
            var (context, page) = await OpenAsync(host, runtime, "transfer");
            await using var _ = context;
            var notes = new string('n', 2_000_000);
            await page.Locator("#forma-notes").FillAsync(notes);

            // Act
            await page.Locator("#forma-save").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#forma-notes-saved")).ToHaveTextAsync(Describe(notes));
            await Assertions.Expect(page.Locator("#saves")).ToHaveTextAsync("1");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_save_the_last_word_typed_into_the_rich_text_editor_inside_its_pause(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "transfer");
            await using var _ = context;
            var canvas = page.Locator("#forma-body .forma-composer-rich-text-canvas");
            await WaitForInputScopeAsync(page, "#forma-body .forma-composer-rich-text-canvas");
            await canvas.PressSequentiallyAsync("Hello world");

            // Act — Save pressed inside the canvas's pause
            await page.Locator("#forma-save").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#forma-body-end")).ToContainTextAsync("Hello world");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_save_a_document_of_megabytes_pasted_into_the_rich_text_editor(string runtime)
        {
            // Arrange — two megabytes pasted into the canvas, its pause not yet over
            var (context, page) = await OpenAsync(host, runtime, "transfer");
            await using var _ = context;
            var canvas = page.Locator("#forma-body .forma-composer-rich-text-canvas");
            await WaitForInputScopeAsync(page, "#forma-body .forma-composer-rich-text-canvas");
            const int Length = 2_000_000;
            await canvas.EvaluateAsync("(canvas, length) => { canvas.focus(); canvas.innerHTML = '<p>' + 'x'.repeat(length) + '</p>'; canvas.dispatchEvent(new Event('input', { bubbles: true })); }", Length);

            // Act
            await page.Locator("#forma-save").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#forma-body-saved")).ToHaveTextAsync(Describe($"<p>{new string('x', Length)}</p>"));
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_submit_a_text_of_megabytes_with_enter_through_a_form_of_the_host_own(string runtime)
        {
            // Arrange — three megabytes in the notes, still on their way as the name is submitted with Enter
            var (context, page) = await OpenAsync(host, runtime, "transfer");
            await using var _ = context;
            var notes = new string('h', 3_000_000);
            await page.Locator("#host-notes").FillAsync(notes);
            await page.Locator("#host-name").FillAsync("Ada");

            // Act
            await page.Locator("#host-name").PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#host-saved")).ToHaveTextAsync($"Ada {Describe(notes)}");
            await Assertions.Expect(page.Locator("#saves")).ToHaveTextAsync("1");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_run_no_save_while_a_text_did_not_arrive_and_save_once_it_does(string runtime)
        {
            // Arrange — the box accepting eight characters handed a hundred past its own limit, which .NET refuses
            var (context, page) = await OpenAsync(host, runtime, "transfer");
            await using var _ = context;
            var code = page.Locator("#forma-code");
            var message = page.Locator("#forma-form .forma-field-message");
            await WaitForInputScopeAsync(page, "#forma-body .forma-composer-rich-text-canvas");
            await code.EvaluateAsync("code => { code.focus(); code.value = 'x'.repeat(100); code.dispatchEvent(new Event('input', { bubbles: true })); code.dispatchEvent(new Event('change', { bubbles: true })); }");
            await Assertions.Expect(message).ToHaveTextAsync("Your text didn't arrive in full. Edit it to try again.");

            // Act — Save pressed while the text can't follow, and again once the box holds a text it accepts
            await page.Locator("#forma-save").ClickAsync();
            await code.FillAsync("abc");
            await Assertions.Expect(message).ToHaveCountAsync(0);
            await page.Locator("#forma-save").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#saves")).ToHaveTextAsync("1");
        }
    }

    public sealed class Channels(BrowserHost host)
    {
        private const int Count = 4000;

        // The menu answers its opening, End and a run once it has drawn, ordered or drawn again every one of its items, an answer the
        // check waits for as it waits for a script's.
        private static readonly LocatorAssertionsToBeFocusedOptions MenuFocus = new() { Timeout = 30_000 };

        private static readonly LocatorAssertionsToHaveTextOptions MenuRun = new() { Timeout = 30_000 };

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_select_a_range_across_thousands_of_items(string runtime)
        {
            // Arrange — the first item pressed
            var (context, page) = await OpenAsync(host, runtime, "transfer-range");
            await using var _ = context;
            await WaitForInputScopeAsync(page, "#list .forma-data-view-region");
            await GetName(page, "Item 1").ClickAsync();
            await Assertions.Expect(page.Locator("#selected")).ToHaveTextAsync("1");

            // Act
            await GetName(page, $"Item {Count}").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

            // Assert
            await Assertions.Expect(page.Locator("#selected")).ToHaveTextAsync($"{Count}");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_reach_the_last_of_thousands_of_menu_items_by_their_order_on_the_page(string runtime)
        {
            // Arrange — the menu opened on its first item
            var (context, page) = await OpenAsync(host, runtime, "transfer-menu");
            await using var _ = context;
            await page.Locator("#menu").ClickAsync();
            await Assertions.Expect(page.Locator("#menu-1")).ToBeFocusedAsync(MenuFocus);
            await WaitForLayerAsync(page.Locator("[popover]").Filter(new() { Has = page.GetByRole(AriaRole.Menu) }));

            // Act — End moves by the items' order the browser answers
            await page.Keyboard.PressAsync("End");
            await Assertions.Expect(page.Locator($"#menu-{Count}")).ToBeFocusedAsync(MenuFocus);
            await page.Keyboard.PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#ran")).ToHaveTextAsync($"Item {Count}", MenuRun);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_reopen_an_image_held_as_a_data_address_past_a_message(string runtime)
        {
            // Arrange — an image of a hundred kilobytes in the document
            var (context, page) = await OpenAsync(host, runtime, "transfer");
            await using var _ = context;
            var canvas = page.Locator("#forma-body .forma-composer-rich-text-canvas");
            await WaitForInputScopeAsync(page, "#forma-body .forma-composer-rich-text-canvas");
            var source = "data:image/png;base64," + new string('A', 100_000);
            await canvas.EvaluateAsync("(canvas, source) => { canvas.innerHTML = '<figure class=\"forma-composer-image\"><img src=\"' + source + '\" alt=\"Large\" /></figure>'; canvas.dispatchEvent(new Event('input', { bubbles: true })); }", source);

            // Act
            await canvas.Locator("img").DblClickAsync();

            // Assert
            var dialog = page.GetByRole(AriaRole.Dialog);
            await Assertions.Expect(dialog.GetByLabel("Source")).ToHaveValueAsync(source);
            await Assertions.Expect(dialog.GetByLabel("Alt Text")).ToHaveValueAsync("Large");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_open_the_link_dialog_on_a_selection_past_a_message(string runtime)
        {
            // Arrange — a hundred kilobytes of text, all of it selected
            var (context, page) = await OpenAsync(host, runtime, "transfer");
            await using var _ = context;
            var canvas = page.Locator("#forma-body .forma-composer-rich-text-canvas");
            await WaitForInputScopeAsync(page, "#forma-body .forma-composer-rich-text-canvas");
            var text = new string('y', 100_000);
            await canvas.EvaluateAsync("(canvas, text) => { canvas.focus(); canvas.innerHTML = '<p>' + text + '</p>'; canvas.dispatchEvent(new Event('input', { bubbles: true })); }", text);
            await canvas.PressAsync("ControlOrMeta+A");
            await page.WaitForFunctionAsync("length => getSelection().toString().length === length", text.Length);

            // Act
            await page.Locator("#forma-body .forma-composer-toolbar").GetByRole(AriaRole.Button, new() { Name = "Insert Link", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Insert Link" }).GetByLabel("Name")).ToHaveValueAsync(text);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_export_a_document_of_megabytes_from_its_preview(string runtime)
        {
            // Arrange — two megabytes in the document, shown in the preview
            var (context, page) = await OpenAsync(host, runtime, "transfer");
            await using var _ = context;
            var canvas = page.Locator("#forma-body .forma-composer-rich-text-canvas");
            await WaitForInputScopeAsync(page, "#forma-body .forma-composer-rich-text-canvas");
            const int Length = 2_000_000;
            await canvas.EvaluateAsync("(canvas, length) => { canvas.focus(); canvas.innerHTML = '<p>' + 'z'.repeat(length) + '</p>'; canvas.dispatchEvent(new Event('input', { bubbles: true })); }", Length);
            await page.Locator("#forma-body").GetByRole(AriaRole.Tab, new() { Name = "Preview", Exact = true }).ClickAsync();
            var export = page.Locator("#forma-body .forma-composer-toolbar").GetByRole(AriaRole.Button, new() { Name = "Export HTML", Exact = true });

            // Act
            var download = await page.RunAndWaitForDownloadAsync(() => export.ClickAsync());

            // Assert — the page the browser composed, the document whole within it
            var exported = await page.EvaluateAsync<int>("address => fetch(address).then(response => response.text()).then(text => text.split('z'.repeat(1000)).length - 1)", download.Url);
            Assert.Equal(Length / 1000, exported);
        }

        // The name as an item of the list holds it.
        private static ILocator GetName(IPage page, string name)
            => page.Locator("#list").Locator(".forma-data-list-item-name", new() { HasTextRegex = new Regex($"^{name}$") });
    }
}
