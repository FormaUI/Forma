namespace Forma.Blazor.Browser.Tests.Components;

using System.Text.RegularExpressions;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The composer's interactions, in every runtime.</summary>
public sealed class ComposerTests
{
    // The picture the checks insert, which the check pages' library serves in every host.
    private const string ImageSource = "_content/Forma.Blazor.Browser.Components.Checks/images/check.svg";

    public sealed class Composer(BrowserHost host)
    {
        // The print a document's frame makes, recorded with what it was given to print.
        private const string RecordingFramePrint = "const contentWindow = Object.getOwnPropertyDescriptor(HTMLIFrameElement.prototype, 'contentWindow'); Object.defineProperty(HTMLIFrameElement.prototype, 'contentWindow', { configurable: true, get() { const framed = contentWindow.get.call(this); if (framed) { const frame = this; framed.print = () => { window.formaPrinted = { title: frame.contentDocument.title, html: frame.contentDocument.body.innerHTML }; }; } return framed; } });";

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_write_the_text_typed_and_made_bold_into_the_bound_document(string runtime)
        {
            // Arrange
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            var bold = GetCommand(page, "Bold");

            // Act — the whole text selected before Bold is pressed
            await SelectTypedAsync(page, canvas, "Hello");
            await bold.ClickAsync();

            // Assert
            await Assertions.Expect(canvas.Locator("b")).ToHaveTextAsync("Hello");
            await Assertions.Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
            await Assertions.Expect(page.Locator("#check .forma-composer-rich-text-announcer")).ToHaveTextAsync("Bold on");
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("<b>Hello</b>");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_link_the_text_selected_through_the_link_dialog(string runtime)
        {
            // Arrange — the whole text selected and the link dialog opened on it
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            await SelectTypedAsync(page, canvas, "Docs");
            await GetCommand(page, "Insert Link").ClickAsync();
            var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Insert Link" });

            // Act
            await dialog.GetByLabel("URL").FillAsync("https://example.com/docs");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(canvas.Locator("a[href='https://example.com/docs']")).ToHaveTextAsync("Docs");
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("href=\"https://example.com/docs\"");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_reopen_a_link_on_a_double_click_with_its_address_text_and_tab(string runtime)
        {
            // Arrange — the text linked to open in a new tab
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            await SelectTypedAsync(page, canvas, "Docs");
            await GetCommand(page, "Insert Link").ClickAsync();
            var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Insert Link" });
            await dialog.GetByLabel("URL").FillAsync("https://example.com/docs");
            await dialog.GetByText("Open in New Tab").ClickAsync();
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();
            await Assertions.Expect(canvas.Locator("a[target=_blank]")).ToHaveTextAsync("Docs");

            // Act
            await canvas.Locator("a").DblClickAsync();

            // Assert
            await Assertions.Expect(dialog.GetByLabel("URL")).ToHaveValueAsync("https://example.com/docs");
            await Assertions.Expect(dialog.GetByLabel("Name", new() { Exact = true })).ToHaveValueAsync("Docs");
            await Assertions.Expect(dialog.GetByRole(AriaRole.Checkbox, new() { Name = "Open in New Tab" })).ToBeCheckedAsync();
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();
            await Assertions.Expect(canvas.Locator("a")).ToHaveCountAsync(1);
            await Assertions.Expect(canvas.Locator("a[target=_blank]")).ToHaveTextAsync("Docs");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_reopen_an_anchor_on_a_double_click_under_its_own_name(string runtime)
        {
            // Arrange — the text made an anchor named apart from it
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            await SelectTypedAsync(page, canvas, "Introduction");
            await GetCommand(page, "Insert Anchor").ClickAsync();
            var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Insert Anchor" });
            await dialog.GetByLabel("Anchor Name").FillAsync("intro");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();
            await Assertions.Expect(canvas.Locator("a[name=intro]")).ToHaveTextAsync("Introduction");

            // Act
            await canvas.Locator("a[name=intro]").DblClickAsync();

            // Assert
            await Assertions.Expect(dialog.GetByLabel("Anchor Name")).ToHaveValueAsync("intro");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();
            await Assertions.Expect(canvas.Locator("a")).ToHaveCountAsync(1);
            await Assertions.Expect(canvas.Locator("a[name=intro]")).ToHaveTextAsync("Introduction");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_report_text_typed_and_take_back_a_command_on_undo(string runtime)
        {
            // Arrange — the text typed reaches the document with no command, then made bold
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            var result = page.Locator("#result");
            await canvas.PressSequentiallyAsync("Hello");
            await Assertions.Expect(result).ToContainTextAsync("Hello");
            await canvas.PressAsync("ControlOrMeta+A");
            await page.WaitForFunctionAsync("() => getSelection().toString() === 'Hello'");
            await GetCommand(page, "Bold").ClickAsync();
            await Assertions.Expect(canvas.Locator("b")).ToHaveTextAsync("Hello");

            // Act
            await GetCommand(page, "Undo").ClickAsync();

            // Assert
            await Assertions.Expect(canvas.Locator("b")).ToHaveCountAsync(0);
            await Assertions.Expect(canvas).ToHaveTextAsync("Hello");
            await Assertions.Expect(result).Not.ToContainTextAsync("<b>");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_set_the_block_style_chosen_from_its_menu(string runtime)
        {
            // Arrange
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            await canvas.PressSequentiallyAsync("Title");

            // Act
            await GetCommand(page, "Block Style").ClickAsync();
            await GetOption(page, "Heading 1").ClickAsync();

            // Assert
            await Assertions.Expect(canvas.Locator("h1")).ToHaveTextAsync("Title");
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("<h1>Title</h1>");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_insert_a_table_with_the_caption_given(string runtime)
        {
            // Arrange
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            await canvas.ClickAsync();
            await GetCommand(page, "Table").ClickAsync();
            await GetOption(page, "Insert Table...").ClickAsync();
            var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Insert Table" });
            await dialog.GetByLabel("Caption").FillAsync("Results");

            // Act
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(canvas.Locator("table caption")).ToHaveTextAsync("Results");
            await Assertions.Expect(canvas.Locator("table thead th").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("<caption>Results</caption>");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_insert_an_image_and_reopen_it_whole_on_a_double_click(string runtime)
        {
            // Arrange — an image inserted with its text and caption
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            await canvas.ClickAsync();
            await GetCommand(page, "Insert Image").ClickAsync();
            var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Insert Image" });
            await dialog.GetByLabel("Source").FillAsync(ImageSource);
            await dialog.GetByLabel("Alt Text").FillAsync("A square");
            await dialog.GetByLabel("Caption").FillAsync("The check's square");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();
            await Assertions.Expect(canvas.Locator("figure.forma-composer-image figcaption")).ToHaveTextAsync("The check's square");
            await Assertions.Expect(canvas.Locator("figure.forma-composer-image img")).Not.ToHaveJSPropertyAsync("naturalWidth", 0);

            // Act
            await canvas.Locator("figure.forma-composer-image img").DblClickAsync();

            // Assert
            var edit = page.GetByRole(AriaRole.Dialog, new() { Name = "Edit Image" });
            await Assertions.Expect(edit.GetByLabel("Source")).ToHaveValueAsync(ImageSource);
            await Assertions.Expect(edit.GetByLabel("Alt Text")).ToHaveValueAsync("A square");
            await edit.GetByLabel("Alt Text").FillAsync("A blue square");
            await edit.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();
            await Assertions.Expect(canvas.Locator("figure.forma-composer-image")).ToHaveCountAsync(1);
            await Assertions.Expect(canvas.Locator("figure.forma-composer-image img")).ToHaveAttributeAsync("alt", "A blue square");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_insert_a_video_and_reopen_it_on_a_double_click(string runtime)
        {
            // Arrange
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            await canvas.ClickAsync();
            await GetCommand(page, "Insert Video").ClickAsync();
            var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Insert Video" });
            await dialog.GetByLabel("Video URL").FillAsync("https://example.com/clip.mp4");
            await dialog.GetByLabel("Title").FillAsync("Clip");
            await dialog.GetByText("Autoplay").ClickAsync();
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();
            var video = canvas.Locator(".forma-composer-video");
            await Assertions.Expect(video).ToHaveAttributeAsync("data-video-url", "https://example.com/clip.mp4");
            await Assertions.Expect(video).ToHaveAttributeAsync("data-autoplay", "true");

            // Act
            await video.DblClickAsync();

            // Assert
            var edit = page.GetByRole(AriaRole.Dialog, new() { Name = "Edit Video" });
            await Assertions.Expect(edit.GetByLabel("Video URL")).ToHaveValueAsync("https://example.com/clip.mp4");
            await Assertions.Expect(edit.GetByLabel("Title")).ToHaveValueAsync("Clip");
            await Assertions.Expect(edit.GetByRole(AriaRole.Checkbox, new() { Name = "Autoplay" })).ToBeCheckedAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_insert_an_embed_from_its_plugin_commands_and_reopen_it_on_a_double_click(string runtime)
        {
            // Arrange
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            await canvas.ClickAsync();
            await GetCommand(page, "Plugin Commands").ClickAsync();
            await GetCommand(page, "Insert Embed").ClickAsync();
            var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Insert Embed" });
            await dialog.GetByLabel("Embed Type").FillAsync("poll");
            await dialog.GetByLabel("Identifier").FillAsync("42");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();
            var embed = canvas.Locator(".forma-composer-embed");
            await Assertions.Expect(embed).ToHaveAttributeAsync("data-embed-id", "42");

            // Act
            await embed.DblClickAsync();

            // Assert
            var edit = page.GetByRole(AriaRole.Dialog, new() { Name = "Edit Embed" });
            await Assertions.Expect(edit.GetByLabel("Embed Type")).ToHaveValueAsync("poll");
            await Assertions.Expect(edit.GetByLabel("Identifier")).ToHaveValueAsync("42");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_its_selection_bar_by_a_selection_and_run_its_commands_there(string runtime)
        {
            // Arrange
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            var bar = page.Locator("#check .forma-composer-selection-bar");

            // Act
            await SelectTypedAsync(page, canvas, "Hello");

            // Assert
            await Assertions.Expect(bar).ToBeVisibleAsync();
            await bar.GetByRole(AriaRole.Button, new() { Name = "Italic", Exact = true }).ClickAsync();
            await Assertions.Expect(canvas.Locator("i")).ToHaveTextAsync("Hello");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_print_and_export_the_document_from_its_preview(string runtime)
        {
            // Arrange — the document's printed frame recorded rather than shown to a headless printer
            var (context, page, canvas) = await OpenComposerAsync(host, runtime, prepare: page => page.AddInitScriptAsync(RecordingFramePrint));
            await using var _ = context;
            await SelectTypedAsync(page, canvas, "Hello");
            await GetCommand(page, "Bold").ClickAsync();
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("<b>Hello</b>");
            await page.Locator("#check").GetByRole(AriaRole.Tab, new() { Name = "Preview", Exact = true }).ClickAsync();

            // Act
            await GetCommand(page, "Print").ClickAsync();
            var download = await page.RunAndWaitForDownloadAsync(() => GetCommand(page, "Export HTML").ClickAsync());

            // Assert
            await page.WaitForFunctionAsync("() => window.formaPrinted?.html.includes('<b>Hello</b>') === true && window.formaPrinted.title === 'Document Preview'");
            Assert.Equal("document.html", download.SuggestedFilename);
            Assert.Contains("<b>Hello</b>", await ReadAsync(page, download), StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_copy_and_download_the_document_s_source(string runtime)
        {
            // Arrange
            var (context, page, canvas) = await OpenComposerAsync(host, runtime, prepare: page => page.AddInitScriptAsync(RecordingClipboard));
            await using var _ = context;
            await AllowClipboardWriteAsync(context, runtime);
            await SelectTypedAsync(page, canvas, "Hello");
            await GetCommand(page, "Bold").ClickAsync();
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("<b>Hello</b>");
            await page.Locator("#check").GetByRole(AriaRole.Tab, new() { Name = "Source", Exact = true }).ClickAsync();

            // Act
            await GetCommand(page, "Copy Source").ClickAsync();
            var download = await page.RunAndWaitForDownloadAsync(() => GetCommand(page, "Download Source").ClickAsync());

            // Assert
            await page.WaitForFunctionAsync("() => (window.formaCopied ?? []).some(text => text.includes('<b>Hello</b>'))");
            Assert.Equal("document.html", download.SuggestedFilename);
            Assert.Contains("<b>Hello</b>", await ReadAsync(page, download), StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_answer_its_own_shortcut_from_its_toolbar_and_move_tab_from_the_canvas_to_its_commands(string runtime)
        {
            // Arrange — the focus in the canvas
            var (context, page, canvas) = await OpenComposerAsync(host, runtime);
            await using var _ = context;
            await WaitForInputRouteAsync(page, "#check", "view.shortcuts");
            await WaitForInputRouteAsync(page, "#check", "focus.next");
            await canvas.FocusAsync();

            // Act
            await page.Keyboard.PressAsync("Tab");
            await page.WaitForFunctionAsync("() => document.getElementById('check').contains(document.activeElement) && !document.activeElement.closest('.forma-composer-canvas')");
            await page.Keyboard.PressAsync("ControlOrMeta+/");

            // Assert
            await Assertions.Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Keyboard Shortcuts" })).ToBeVisibleAsync();
        }
    }

    public sealed class ComposerPlugins(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_load_the_module_and_the_stylesheet_a_host_s_plugin_declares(string runtime)
        {
            // Arrange
            var (context, page, _) = await OpenComposerAsync(host, runtime, "composer-plugins");
            await using var __ = context;

            // Act
            var stylesheets = page.Locator("link[rel=stylesheet][href^='data:text/css']");

            // Assert
            await page.WaitForFunctionAsync("() => window.formaCheckModule === 1");
            await Assertions.Expect(stylesheets).ToHaveCountAsync(1);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_stand_a_plugin_s_marker_beside_its_block_and_bring_the_caret_there_when_pressed(string runtime)
        {
            // Arrange — the host's plugin marks the second paragraph
            var (context, page, canvas) = await OpenComposerAsync(host, runtime, "composer-plugins");
            await using var _ = context;
            await GetCommand(page, "Plugin Commands").ClickAsync();
            await GetCommand(page, "Mark Second").ClickAsync();
            var marker = page.Locator("#check .forma-composer-rich-text-gutter [data-block='1']");

            // A row shows once the gutter's script has placed it beside its block, an answer that waits as long as every script's.
            await Assertions.Expect(marker).ToBeVisibleAsync(new() { Timeout = 30000 });

            // Act
            await marker.GetByRole(AriaRole.Button).ClickAsync();
            await page.WaitForFunctionAsync("() => { const selection = getSelection(); const second = document.querySelector('#check .forma-composer-rich-text-canvas').children[1]; return selection.isCollapsed && second.contains(selection.anchorNode); }");
            await page.Keyboard.TypeAsync("X");

            // Assert
            await Assertions.Expect(canvas.Locator("p").Nth(1)).ToHaveTextAsync(new Regex("X"));
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("Second");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_insert_what_a_host_s_plugin_hands_it(string runtime)
        {
            // Arrange
            var (context, page, canvas) = await OpenComposerAsync(host, runtime, "composer-plugins");
            await using var _ = context;
            await canvas.ClickAsync();
            await GetCommand(page, "Plugin Commands").ClickAsync();

            // Act
            await GetCommand(page, "Insert Picture").ClickAsync();

            // Assert
            var picture = canvas.Locator($"img.forma-composer-image[src='{ImageSource}']");
            await Assertions.Expect(picture).ToHaveAttributeAsync("alt", "check.svg");
            await Assertions.Expect(picture).Not.ToHaveJSPropertyAsync("naturalWidth", 0);
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync(ImageSource);
        }
    }

    public sealed class ComposerPicker(BrowserHost host)
    {
        // The names the dialog lists at the top level: the top-level folders, then every picture across every folder, the document left out.
        private static readonly string[] TopLevelNames = ["Ceremonies", "Stages", "first.svg", "locked.svg", "podium.svg", "second.svg", "sprint.svg", "start.svg"];

        // Presses Enter on the box holding the focus and Insert in the same turn, so Insert arrives while the confirmation Enter asked for
        // is under way.
        private const string ActivateAndInsert = """
            () => {
                const dialog = document.querySelector('dialog[open]');
                document.activeElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
                [...dialog.querySelectorAll('button')].find(button => button.textContent.trim() === 'Insert').click();
            }
            """;

        // Opens the stored-files dialog on the composer, the caret in its canvas, once its folders are read and its items take presses.
        private static async Task<(IBrowserContext Context, IPage Page, ILocator Dialog)> OpenPickerAsync(BrowserHost host, string runtime)
        {
            var (context, page, canvas) = await OpenComposerAsync(host, runtime, "composer-picker");

            try
            {
                await canvas.ClickAsync();
                await GetCommand(page, "Browse Files").ClickAsync();
                var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Browse Files" });
                await Assertions.Expect(dialog.Locator(".forma-data-card-name")).ToHaveTextAsync(TopLevelNames);
                await WaitForInputScopeAsync(page, "dialog[open] .forma-data-view-region");

                return (context, page, dialog);
            }
            catch
            {
                await context.DisposeAsync();

                throw;
            }
        }

        private static ILocator GetName(ILocator dialog, string name)
            => dialog.Locator(".forma-data-card-name").GetByText(name, new() { Exact = true });

        // The box on the card showing the name, the card found by a name looked for within it.
        private static ILocator GetBox(ILocator dialog, string name)
            => dialog.Locator(".forma-data-card")
                .Filter(new() { Has = dialog.Page.Locator(".forma-data-card-name").GetByText(name, new() { Exact = true }) })
                .Locator("input[type=checkbox]");

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_choose_a_file_in_place_of_another_and_enter_a_folder_to_reach_the_one_it_inserts(string runtime)
        {
            // Arrange
            var (context, page, dialog) = await OpenPickerAsync(host, runtime);
            await using var _ = context;

            // Act — first.svg chosen, second.svg in its place, and the disabled locked.svg pressed
            await GetName(dialog, "first.svg").ClickAsync();
            await Assertions.Expect(GetBox(dialog, "first.svg")).ToBeCheckedAsync();
            await GetName(dialog, "second.svg").ClickAsync();
            await Assertions.Expect(GetBox(dialog, "second.svg")).ToBeCheckedAsync();
            await Assertions.Expect(GetBox(dialog, "first.svg")).Not.ToBeCheckedAsync();
            await GetName(dialog, "locked.svg").ClickAsync();
            await Assertions.Expect(GetBox(dialog, "locked.svg")).Not.ToBeCheckedAsync();
            await Assertions.Expect(GetBox(dialog, "second.svg")).ToBeCheckedAsync();

            // Act — a press on Ceremonies, whose box can't be checked, enters it, and its picture is chosen and inserted
            await Assertions.Expect(GetBox(dialog, "Ceremonies")).ToHaveAttributeAsync("aria-disabled", "true");
            await GetName(dialog, "Ceremonies").ClickAsync();
            await Assertions.Expect(dialog.Locator(".forma-data-card-name")).ToHaveTextAsync(["podium.svg"]);
            await GetName(dialog, "podium.svg").ClickAsync();
            await Assertions.Expect(GetBox(dialog, "podium.svg")).ToBeCheckedAsync();
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(dialog).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("?name=podium.svg");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_insert_the_file_activated_once_while_insert_is_pressed_and_hand_the_focus_back(string runtime)
        {
            // Arrange — first.svg chosen, the focus on second.svg's box
            var (context, page, dialog) = await OpenPickerAsync(host, runtime);
            await using var _ = context;
            await GetName(dialog, "first.svg").ClickAsync();
            await Assertions.Expect(GetBox(dialog, "first.svg")).ToBeCheckedAsync();
            await GetBox(dialog, "second.svg").FocusAsync();

            // Act — Enter on second.svg, and Insert pressed while that confirmation is under way
            await page.EvaluateAsync(ActivateAndInsert);

            // Assert — second.svg inserted once, first.svg not at all, and the focus back on the command that opened the dialog
            await Assertions.Expect(dialog).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("?name=second.svg");
            Assert.Equal(1, await page.EvaluateAsync<int>("() => document.querySelector('#result').textContent.split('?name=second.svg').length - 1"));
            await Assertions.Expect(page.Locator("#result")).Not.ToContainTextAsync("?name=first.svg");
            await Assertions.Expect(GetCommand(page, "Browse Files")).ToBeFocusedAsync();
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_stay_open_and_usable_after_refusing_to_insert_nothing(string runtime)
        {
            // Arrange
            var (context, page, dialog) = await OpenPickerAsync(host, runtime);
            await using var _ = context;
            var insert = dialog.GetByRole(AriaRole.Button, new() { Name = "Insert", Exact = true });

            // Act — Insert with nothing chosen, then a file chosen and Insert again
            await insert.ClickAsync();
            await Assertions.Expect(dialog.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Choose a file before inserting.");
            await GetName(dialog, "start.svg").ClickAsync();
            await Assertions.Expect(GetBox(dialog, "start.svg")).ToBeCheckedAsync();
            await insert.ClickAsync();

            // Assert
            await Assertions.Expect(dialog).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#result")).ToContainTextAsync("?name=start.svg");
        }
    }

    public sealed class ComposerPresets(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_the_text_each_preset_makes_bold_and_offer_its_own_plugins(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "composer-presets");
            await using var _ = context;

            foreach (var preset in new[] { "standard", "compact" })
            {
                var canvas = page.Locator($"#{preset} .forma-composer-rich-text-canvas");
                await WaitForInputScopeAsync(page, $"#{preset} .forma-composer-rich-text-canvas");

                // Act — the whole text selected before Bold is pressed
                await SelectTypedAsync(page, canvas, "Hello");
                await GetCommand(page, "Bold", preset).ClickAsync();

                // Assert
                await Assertions.Expect(page.Locator($"#{preset}-result")).ToContainTextAsync("<b>Hello</b>");
            }

            await Assertions.Expect(GetCommand(page, "Table", "standard")).ToBeVisibleAsync();
            await Assertions.Expect(GetCommand(page, "Table", "compact")).ToHaveCountAsync(0);
        }
    }

    public sealed class ComposerPlain(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_text_typed_and_take_it_back_on_undo(string runtime)
        {
            // Arrange — the text entered in one input, which commits as one history state
            var (context, page) = await OpenAsync(host, runtime, "composer-plain");
            await using var _ = context;
            var editor = page.Locator("#check textarea");
            await WaitForInputRouteAsync(page, "#check textarea", "composer.undo");
            await editor.FillAsync("abc");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("abc");

            // Act
            await editor.PressAsync("ControlOrMeta+Z");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync(string.Empty);
            await Assertions.Expect(editor).ToHaveValueAsync(string.Empty);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_take_plain_text_only_as_its_edit_commits_when_it_updates_on_change(string runtime)
        {
            // Arrange — text typed and not yet taken
            var (context, page) = await OpenAsync(host, runtime, "composer-plain");
            await using var _ = context;
            var editor = page.Locator("#committed textarea");
            await editor.PressSequentiallyAsync("abc");
            await MakeRoundTripAsync(page);
            await Assertions.Expect(page.Locator("#committed-result")).ToHaveTextAsync(string.Empty);

            // Act
            await editor.PressAsync("Tab");

            // Assert
            await Assertions.Expect(page.Locator("#committed-result")).ToHaveTextAsync("abc");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_commit_the_plain_text_typed_before_a_shortcut_runs_when_it_updates_on_change(string runtime)
        {
            // Arrange — one edit committed, then more typed and not yet taken
            var (context, page) = await OpenAsync(host, runtime, "composer-plain");
            await using var _ = context;
            var editor = page.Locator("#committed textarea");
            await WaitForInputRouteAsync(page, "#committed textarea", "composer.undo");
            await editor.PressSequentiallyAsync("abc");
            await editor.PressAsync("Tab");
            await Assertions.Expect(page.Locator("#committed-result")).ToHaveTextAsync("abc");
            await editor.FocusAsync();
            await editor.PressAsync("End");
            await editor.PressSequentiallyAsync("xyz");

            // Act — the undo commits what was typed and then undoes it
            await editor.PressAsync("ControlOrMeta+Z");

            // Assert
            await Assertions.Expect(page.Locator("#committed-result")).ToHaveTextAsync("abc");
            await Assertions.Expect(editor).ToHaveValueAsync("abc");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_report_the_selection_made_in_its_text(string runtime)
        {
            // Arrange — the first word selected by the keys
            var (context, page) = await OpenAsync(host, runtime, "composer-plain");
            await using var _ = context;
            var editor = page.Locator("#check textarea");
            await editor.FillAsync("Hello world");
            await editor.PressAsync("Home");
            for (var step = 0; step < 5; step++)
            {
                await editor.PressAsync("Shift+ArrowRight");
            }

            await GetCommand(page, "Plugin Commands").ClickAsync();

            // Act — the composer settles the editor before it runs a command, which reads the selection as the textarea holds it
            await GetCommand(page, "Show Selection").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#check .forma-toolbar-readout", new() { HasText = "0-5" })).ToHaveCountAsync(1);
        }
    }

    // The page's composer, once its canvas answers input; a check that fails there ends, giving its runtime's turn to the next.
    private static async Task<(IBrowserContext Context, IPage Page, ILocator Canvas)> OpenComposerAsync(BrowserHost host, string runtime, string path = "composer", Func<IPage, Task>? prepare = null)
    {
        var (context, page) = await OpenAsync(host, runtime, path, prepare);
        var canvas = page.Locator("#check .forma-composer-rich-text-canvas");

        try
        {
            await WaitForInputScopeAsync(page, "#check .forma-composer-rich-text-canvas");
        }
        catch
        {
            await context.DisposeAsync();

            throw;
        }

        return (context, page, canvas);
    }

    // The text typed into the canvas, then all of it selected.
    private static async Task SelectTypedAsync(IPage page, ILocator canvas, string text)
    {
        await canvas.PressSequentiallyAsync(text);
        await canvas.PressAsync("ControlOrMeta+A");
        await page.WaitForFunctionAsync("text => getSelection().toString() === text", text);
    }

    // A command of the composer the page's element of that id holds in its toolbar.
    private static ILocator GetCommand(IPage page, string name, string composer = "check")
        => page.Locator($"#{composer} .forma-composer-toolbar").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static ILocator GetOption(IPage page, string name)
        => page.Locator("[role=menuitem], [role=menuitemradio], [role=menuitemcheckbox]").Filter(new() { HasText = name });

    // The text a page offered for download, read in the page from the address it offered it at.
    private static Task<string> ReadAsync(IPage page, IDownload download)
        => page.EvaluateAsync<string>("address => fetch(address).then(response => response.text())", download.Url);
}
