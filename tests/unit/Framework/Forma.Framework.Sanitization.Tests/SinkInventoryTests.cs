namespace Forma.Framework.Sanitization.Tests;

using System.Text.RegularExpressions;

using Forma.Tests.Shared;

using Xunit;

/// <summary>
/// Every place the library writes markup, an address or a style into a page, found from its source: an address or a style
/// attribute bound to data or a <c>MarkupString</c> in a component, a style a render tree writes or a style builder adds as
/// text, and markup a script parses or writes, an address it sets, loads or navigates to, or a style it sets. Each is
/// covered by a test giving it hostile input, takes data only as a typed value, or writes what no data reaches.
/// </summary>
public sealed partial class SinkInventoryTests
{
    private const string BlazorTests = "tests/bunit/Blazor/Forma.Blazor.BUnit.Tests/Components";
    private const string ComposerTests = "tests/bunit/Blazor/Forma.Blazor.Composer.BUnit.Tests";
    private const string ExplorerTests = "tests/bunit/Blazor/Forma.Blazor.Explorer.BUnit.Tests/FormaExplorerTests.razor";
    private const string Components = "src/Blazor/Forma.Blazor/Components";
    private const string Composer = "src/Blazor/Forma.Blazor.Composer";
    private const string BrowserScripts = "src/Framework/Forma.Framework.Browser.Interop/wwwroot/scripts";
    private const string DataView = "src/Blazor/Forma.Blazor.DataView";
    private const string Charts = "src/Blazor/Forma.Blazor.Charts/FormaChartFrame.razor";

    // Each sink, as its file and what it writes, with the test that gives it hostile input, as its file and its name.
    private static readonly Dictionary<string, string> CoveredSinks = new(StringComparer.Ordinal)
    {
        [$"{Components}/Buttons/FormaButton.razor | href=@LinkHref"] = $"{BlazorTests}/Buttons/FormaButtonTests.razor | Should_write_an_address_a_link_refuses_inert",
        [$"{Components}/Files/FormaFileViewer.razor | (MarkupString)markup.Html"] = "tests/unit/AddOns/Forma.AddOns.Resources.Markdown.Tests/MarkdownResourceProviderTests.cs | Should_drop_raw_html_a_document_embeds",
        [$"{Components}/Files/FormaFileViewer.razor | src=@ImageAddress"] = $"{BlazorTests}/Files/FormaFileViewerTests.razor | Should_write_a_source_its_kind_refuses_inert",
        [$"{Components}/Files/FormaFileViewer.razor | src=@AudioAddress"] = $"{BlazorTests}/Files/FormaFileViewerTests.razor | Should_write_a_source_its_kind_refuses_inert",
        [$"{Components}/Files/FormaFileViewer.razor | src=@DocumentAddress"] = $"{BlazorTests}/Files/FormaFileViewerTests.razor | Should_write_a_source_its_kind_refuses_inert",
        [$"{Components}/Files/FormaFileViewer.razor | data=@DocumentAddress"] = $"{BlazorTests}/Files/FormaFileViewerTests.razor | Should_write_a_source_its_kind_refuses_inert",
        [$"{Components}/Files/FormaFileViewer.razor | href=@DocumentLinkAddress"] = $"{BlazorTests}/Files/FormaFileViewerTests.razor | Should_write_a_document_s_link_an_address_a_link_refuses_inert",
        [$"{Components}/Files/FormaFileViewer.razor | href=@DownloadAddress"] = $"{BlazorTests}/Files/FormaFileViewerTests.razor | Should_write_a_download_address_its_kind_refuses_inert",
        [$"{Components}/Media/FormaVideoPlayer.razor | src=@placeholder"] = $"{BlazorTests}/Media/FormaVideoPlayerTests.razor | Should_show_no_placeholder_at_an_address_an_image_refuses",
        [$"{Components}/Media/FormaVideoPlayer.razor | poster=@PlaceholderAddress"] = $"{BlazorTests}/Media/FormaVideoPlayerTests.razor | Should_show_no_placeholder_at_an_address_an_image_refuses",
        [$"{Components}/Media/FormaVideoPlayer.razor | src=@nativeFrame.Source"] = $"{BlazorTests}/Media/FormaVideoPlayerTests.razor | Should_show_the_video_unavailable_where_a_host_s_file_frame_is_at_an_address_a_video_refuses",
        [$"{Components}/Media/FormaVideoPlayer.razor | src=@GetCaptionAddress(caption)"] = $"{BlazorTests}/Media/FormaVideoPlayerTests.razor | Should_write_a_caption_track_at_an_address_a_track_refuses_inert",
        [$"{Components}/Media/FormaVideoPlayer.razor | src=@embeddedFrame.Source"] = $"{BlazorTests}/Media/FormaVideoPlayerTests.razor | Should_show_the_video_unavailable_where_a_host_s_embedded_frame_is_at_an_address_a_frame_refuses",
        [$"{Components}/Media/FormaVideoPlayer.razor | (MarkupString)markup.Html"] = $"{BlazorTests}/Media/FormaVideoPlayerTests.razor | Should_write_a_service_s_markup_with_no_part_of_a_hostile_address_as_markup",
        [$"{Components}/Media/FormaVideoPlayer.razor | href=@FallbackAddress"] = $"{BlazorTests}/Media/FormaVideoPlayerTests.razor | Should_write_a_service_s_fallback_link_inert_where_a_link_refuses_its_address",
        [$"{Components}/Menus/FormaDropDownMenuItem.razor | href=@_linkAddress"] = $"{BlazorTests}/Menus/FormaDropDownMenuTests.razor | Should_write_an_address_a_link_refuses_inert_and_keep_the_item_in_the_tab_order",
        [$"{Components}/Menus/FormaShareMenu.razor | href=@address"] = $"{BlazorTests}/Menus/FormaShareMenuTests.razor | Should_draw_a_target_whose_address_a_link_refuses_as_a_disabled_command_on_a_static_page",
        [$"{Components}/Menus/FormaShareMenu.razor | href=@SheetResourceAddress"] = $"{BlazorTests}/Menus/FormaShareMenuTests.razor | Should_write_a_download_address_its_kind_refuses_inert",
        [$"{Components}/Menus/FormaShareMenu.razor | href=@EmailAddress"] = $"{BlazorTests}/Menus/FormaShareMenuTests.razor | Should_carry_a_hostile_name_and_address_in_the_mail_it_offers_as_text_alone",
        [$"{Components}/Navigation/FormaBreadcrumb.razor | href=@_itemAddresses[index]"] = $"{BlazorTests}/Navigation/FormaBreadcrumbTests.razor | Should_write_an_address_a_link_refuses_inert",
        [$"{Components}/Navigation/FormaLinkTabs.razor | href=@_tabAddresses[index]"] = $"{BlazorTests}/Navigation/FormaLinkTabsTests.razor | Should_write_an_address_a_link_refuses_inert",
        [$"{Components}/Navigation/FormaStepper.razor | href=@address"] = $"{BlazorTests}/Navigation/FormaStepperTests.razor | Should_draw_a_step_whose_address_a_link_refuses_without_a_link",
        [$"{Components}/Symbols/FormaEmoji.razor | href=@GetSpriteHref(sprite)"] = $"{BlazorTests}/Symbols/FormaEmojiTests.razor | Should_draw_an_emoji_whose_sheet_an_image_refuses_as_its_own_character_and_load_no_sheet",
        [$"{Components}/Symbols/FormaEmoji.razor | src=@image.Url"] = $"{BlazorTests}/Symbols/FormaEmojiTests.razor | Should_draw_an_emoji_whose_image_an_image_refuses_as_its_own_character",
        [$"{Composer}/FormaComposerAttachments.razor | href=@GetAttachmentAddress(attachment)"] = $"{ComposerTests}/FormaComposerTests.razor | Should_write_an_attachment_s_address_a_link_refuses_inert",
        [$"{Composer}/RichTextEditor/FormaRichTextEditor.razor | (MarkupString)_canvasText"] = $"{ComposerTests}/RichTextEditor/FormaRichTextEditorTests.razor | Should_show_a_hostile_document_only_as_the_sanitizer_keeps_it_under_a_static_render",
        [$"{Composer}/RichTextEditor/Views/FormaRichTextPreview.razor | (MarkupString)Content"] = $"{ComposerTests}/FormaComposerTests.razor | Should_preview_and_print_a_hostile_document_only_as_the_sanitizer_keeps_it",
        [$"{Composer}/wwwroot/scripts/RichTextEditor/Canvas.js | this.#element.innerHTML = value"] = $"{ComposerTests}/RichTextEditor/FormaRichTextEditorTests.razor | Should_write_a_template_into_the_canvas_only_as_the_sanitizer_keeps_it",
        [$"{Composer}/wwwroot/scripts/RichTextEditor/ContentWriter.js | template.innerHTML = html"] = $"{ComposerTests}/RichTextEditor/FormaRichTextEditorTests.razor | Should_write_a_template_into_the_canvas_only_as_the_sanitizer_keeps_it",
        ["src/Blazor/Forma.Blazor.Explorer/FormaExplorerThumbnail.razor | src=@still"] = $"{ExplorerTests} | Should_draw_its_glyph_for_a_still_at_an_address_an_image_refuses",
        ["src/Blazor/Forma.Blazor.Explorer/FormaExplorerThumbnail.razor | src=@frame"] = $"{ExplorerTests} | Should_draw_no_frame_of_a_video_at_an_address_a_video_refuses",
        [$"{BrowserScripts}/operations/Asset.js | .parseFromString(await response.text(), 'image/svg+xml')"] = $"{BlazorTests}/Symbols/FormaEmojiTests.razor | Should_draw_an_emoji_whose_sheet_an_image_refuses_as_its_own_character_and_load_no_sheet",
        [$"{BrowserScripts}/operations/Print.js | article.innerHTML = html"] = $"{ComposerTests}/FormaComposerTests.razor | Should_preview_and_print_a_hostile_document_only_as_the_sanitizer_keeps_it",
        [$"{BrowserScripts}/operations/Print.js | printedDocument.body.firstElementChild.innerHTML = html"] = $"{ComposerTests}/FormaComposerTests.razor | Should_preview_and_print_a_hostile_document_only_as_the_sanitizer_keeps_it",
        ["src/Framework/Forma.Framework.JavaScript.Interop/wwwroot/scripts/Sanitizer.js | template.innerHTML = html ?? ''"] = "tests/scripts/core/Sanitizer.test.js | drops a script and an image error handler from markup",
        ["src/Framework/Forma.Framework.JavaScript.Interop/wwwroot/scripts/Sanitizer.js | setAttribute('style', `${keptDeclarations.join(';').trim()};`)"] = "tests/scripts/core/Sanitizer.test.js | drops a style rule that fetches an address or runs an expression",
        [$"{Components}/Bars/FormaToolbar.razor | style=@option.PreviewStyle"] = "tests/unit/Blazor/Forma.Blazor.Composer.Tests/RichTextEditor/RichTextPluginTests.cs | Should_preview_only_a_font_family_list_on_its_own_label",
        [$"{Components}/Inputs/FormaColorPicker.razor | style=@SwatchStyle"] = $"{BlazorTests}/Inputs/FormaColorPickerTests.razor | Should_draw_no_swatch_for_a_value_that_is_not_a_hex_color",
    };

    // The sinks no data reaches, each with what it writes.
    private static readonly Dictionary<string, string> UnreachedSinks = new(StringComparer.Ordinal)
    {
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@DocumentBase"] = "The page's base address, which the host configures",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@IconHref"] = "The page's icon, which the host configures",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[TokensStylesheet]"] = "The library's own stylesheet",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[theme.Stylesheet]"] = "A registered theme's stylesheet, which the host's registration names",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[\"_content/Forma.Blazor/forma-reset.css\"]"] = "The library's own stylesheet",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[\"_content/Forma.Blazor/forma-base.css\"]"] = "The library's own stylesheet",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[\"_content/Forma.Blazor/forma-input.css\"]"] = "The library's own stylesheet",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[\"_content/Forma.Blazor/forma-prose.css\"]"] = "The library's own stylesheet",
        [$"{BrowserScripts}/behaviors/FilePreview.js | image.src = url"] = "A blob address the script makes of a file the reader picked",
        [$"{BrowserScripts}/operations/Download.js | link.href = url"] = "A blob address the script makes of the text it saves",
        [$"{BrowserScripts}/operations/Asset.js | import(address)"] = "A module a composer plugin declares in its code",
        [$"{BrowserScripts}/operations/Asset.js | script.src = url"] = "A script a player provider declares in its code",
        [$"{BrowserScripts}/operations/Asset.js | link.href = url"] = "A stylesheet a composer plugin declares in its code",
        [$"{BrowserScripts}/operations/Print.js | printedDocument.write(`<!DOCTYPE html><html lang=\"${{document.documentElement.lang}}\" dir=\"${{document.documentElement.dir}}\"><head><meta charset=\"utf-8\"><title></title></head><body><article></article></body></html>`)"] = "The library's own document, in the page's own language and direction",
        [$"{BrowserScripts}/operations/Print.js | link.href = href"] = "The page's own stylesheets",
        ["src/Framework/Forma.Framework.Culture.Browser.Interop/wwwroot/scripts/CultureCoordinator.js | location.assign(destination)"] = "The page's own address in another culture",
        [$"{Components}/Themes/FormaThemePicker.razor | action=@SwitchAddress"] = "The theme system's own endpoint, relative to the document's base",
        ["src/Blazor/Forma.Blazor/Components/FormaElementBase.cs | builder.AddDeclarations(Style);"] = "The host's own style, which is the host's as its Class is",
        ["src/Blazor/Forma.Blazor/Components/FormaInputBase`1.cs | builder.AddDeclarations(Style);"] = "The host's own style, which is the host's as its Class is",
        [$"{BrowserScripts}/operations/Asset.js | host.style.cssText = 'position:absolute;inline-size:0;block-size:0;overflow:hidden'"] = "The library's own declarations for the element holding the sprites it loads",
        [$"{BrowserScripts}/operations/Print.js | frame.style.position = 'fixed'"] = "The library's own declarations for the frame it prints in",
        [$"{BrowserScripts}/operations/Print.js | frame.style.inset = '0'"] = "The library's own declarations for the frame it prints in",
        [$"{BrowserScripts}/operations/Print.js | frame.style.width = '0'"] = "The library's own declarations for the frame it prints in",
        [$"{BrowserScripts}/operations/Print.js | frame.style.height = '0'"] = "The library's own declarations for the frame it prints in",
        [$"{BrowserScripts}/operations/Print.js | frame.style.border = '0'"] = "The library's own declarations for the frame it prints in",
        [$"{BrowserScripts}/operations/Print.js | frame.style.visibility = 'hidden'"] = "The library's own declarations for the frame it prints in",
        ["src/Blazor/Forma.Blazor.DataView/wwwroot/scripts/HeightReservation.js | this.#target.style.minBlockSize = ''"] = "No height, which releases the one reserved",
    };

    // The style sinks data reaches only as a typed value, each with the value that carries it: a declaration whose value is a CSS length, a
    // color or a number the library formats itself can't hold a declaration of its own, and a script setting one property through the
    // element's declarations hands the browser a value it parses for that property alone.
    private static readonly Dictionary<string, string> TypedSinks = new(StringComparer.Ordinal)
    {
        [Components + "/Bars/FormaToolbar.razor | style=@($\"background-color:{activeSwatch}\")"] = "An option's swatch, a CssColorValue",
        [Components + "/Bars/FormaToolbar.razor | style=@($\"background-color:{swatch}\")"] = "An option's swatch, a CssColorValue",
        [Components + "/Files/FormaFileViewer.razor | style=@(IsZoomedIn ? $\"inline-size: {ZoomPercent}%;\" : null)"] = "The zoom, a whole number of percent",
        [$"{Components}/Inputs/FormaDateBox.razor | style=@TriggerStyle"] = "The root style of the date picker drawing the box, its own lengths and the host's style",
        [$"{Components}/Inputs/FormaNumberInput.razor | style=@FieldStyle"] = "The input's width, a CssLength",
        [$"{Components}/Inputs/FormaTextBox.razor | style=@FieldStyle"] = "The box's width, a CssLength",
        [$"{Components}/Inputs/FormaTimeBox.razor | style=@InputStyle"] = "The root style of the time picker drawing the box, its own lengths and the host's style",
        [$"{Components}/Virtualization/FormaVirtualize.razor.cs | builder.AddAttribute(3, \"style\", string.Create(CultureInfo.InvariantCulture, $\"block-size: {{drawn.Height:0.##}}px; flex-shrink: 0; overflow-anchor: none; --forma-virtualize-item-size: {{CurrentItemSize:0.##}}px\"));"] = "Heights the virtualizer measures, numbers it formats itself",
        [$"{Charts} | style=@GetBarStyle(value)"] = "Fractions of the chart's scale, numbers it formats as percentages",
        [$"{Charts} | style=@GetHeightStyle(threshold.Value)"] = "A fraction of the chart's scale, a number it formats as a percentage",
        [$"{Charts} | style=@GetHeightStyle(tick)"] = "A fraction of the chart's scale, a number it formats as a percentage",
        [$"{Charts} | style=@GetHeightStyle(value)"] = "A fraction of the chart's scale, a number it formats as a percentage",
        [$"{Charts} | style=@GetPeriodStyle(periodIndex)"] = "Fractions of the chart's timeline, numbers it formats as percentages",
        [$"{Charts} | style=@GetTimeLabelStyle(label)"] = "A fraction of the chart's timeline, a number it formats as a percentage",
        [$"{Charts} | style=@ValueGutterStyle"] = "The value labels' gutter, a CssLength",
        [$"{Composer}/RichTextEditor/Views/FormaRichTextPreview.razor | style=@PageStyle"] = "The page's size and zoom, numbers the preview formats itself",
        [$"{DataView}/FormaDataCards.razor | style=@PlaceholderStyle"] = "A row's height, a number the view formats itself",
        [$"{DataView}/FormaDataGridRow.razor | style=@Grid.GetCellStyle(column)"] = "A frozen column's offset and width, each a CssLength",
        [$"{DataView}/FormaDataGridTable.razor | style=@Grid.GetCellStyle(column)"] = "A frozen column's offset and width, each a CssLength",
        [$"{DataView}/FormaDataGridTable.razor | style=@Grid.GetHeaderStyle(column)"] = "A column's width and a frozen column's offset, each a CssLength",
        [$"{DataView}/FormaDataGridTable.razor | style=@Grid.PlaceholderStyle"] = "A row's height, a number the grid formats itself",
        [$"{DataView}/FormaDataList.razor | style=@PlaceholderStyle"] = "A row's height, a number the view formats itself",
        [$"{DataView}/FormaDataTreeNode.razor | style=@(FormaDataTree<TItem>.GetNodeStyle(Depth))"] = "A node's depth, a whole number",
        ["src/Blazor/Forma.Blazor.Explorer/FormaExplorer.razor | style=@SidebarSizeStyle"] = "The sidebar's width, a CssLength",
        ["src/Blazor/Forma.Blazor.Explorer/FormaExplorerSidebar.razor | style=@TreeSizeStyle"] = "The folder tree's height, a CssLength",
        [$"{Composer}/wwwroot/scripts/BlockAlignment.js | row.style.insetBlockStart = `${{block.getBoundingClientRect().top - origin}}px`"] = "An offset the script measures, in pixels",
        [$"{Composer}/wwwroot/scripts/RichTextEditor/BlockFormatter.js | block.style.lineHeight = value"] = "A line height a command carries, which the browser takes for that one property alone",
        [$"{Composer}/wwwroot/scripts/RichTextEditor/BlockFormatter.js | block.style.textAlign = value"] = "An alignment a command carries, which the browser takes for that one property alone",
        [$"{Composer}/wwwroot/scripts/RichTextEditor/InlineFormatter.js | pendingSpan.style.setProperty(property, value)"] = "A font, a size or a color a command carries, which the browser takes for that one property alone",
        [$"{Composer}/wwwroot/scripts/RichTextEditor/InlineFormatter.js | span.style.setProperty(property, value)"] = "A font, a size or a color a command carries, which the browser takes for that one property alone",
        [$"{Composer}/wwwroot/scripts/RichTextEditor/ListFormatter.js | block.style.marginInlineStart = `${{nextIndent}}px`"] = "An indent the script counts, in pixels",
        [$"{Composer}/wwwroot/scripts/SelectionFollower.js | this.#target.style.left = `${{left}}px`"] = "A position the script measures, in pixels",
        [$"{Composer}/wwwroot/scripts/SelectionFollower.js | this.#target.style.top = `${{top}}px`"] = "A position the script measures, in pixels",
        [$"{DataView}/wwwroot/scripts/HeightReservation.js | this.#target.style.minBlockSize = `${{this.#reservedHeight}}px`"] = "A height the script measures, in pixels",
        [$"{BrowserScripts}/behaviors/AnchoredPopover.js | this.#popover.style.left = `${{Math.round(inlineStart)}}px`"] = "A position the script measures, in pixels",
        [$"{BrowserScripts}/behaviors/AnchoredPopover.js | this.#popover.style.top = `${{Math.round(blockStart)}}px`"] = "A position the script measures, in pixels",
        [$"{BrowserScripts}/behaviors/AnchoredPopover.js | this.#popover.style.setProperty('--forma-popover-anchor-inline-size', `${{Math.round(anchor.width)}}px`)"] = "A width the script measures, in pixels",
        [$"{BrowserScripts}/components/Frame.js | this.#box.style.left = `${{left}}px`"] = "A position the script measures, in pixels",
        [$"{BrowserScripts}/components/Frame.js | this.#box.style.top = `${{top}}px`"] = "A position the script measures, in pixels",
        [$"{BrowserScripts}/components/Frame.js | this.#box.style.width = `${{width}}px`"] = "A size the script measures, in pixels",
        [$"{BrowserScripts}/components/Frame.js | this.#box.style.height = `${{height}}px`"] = "A size the script measures, in pixels",
    };

    [Fact]
    public void Should_cover_every_sink_the_source_writes()
    {
        // Act
        var uncoveredSinks = FindSinks()
            .Where(sink => !CoveredSinks.ContainsKey(sink) && !UnreachedSinks.ContainsKey(sink) && !TypedSinks.ContainsKey(sink))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // Assert
        Assert.True(
            uncoveredSinks.Count == 0,
            $"These sinks write markup, an address or a style with no test giving them hostile input:{Environment.NewLine}{string.Join(Environment.NewLine, uncoveredSinks)}");
    }

    [Fact]
    public void Should_list_only_sinks_the_source_still_writes()
    {
        // Arrange
        var sinks = FindSinks().ToHashSet(StringComparer.Ordinal);

        // Act
        var staleSinks = CoveredSinks.Keys.Concat(UnreachedSinks.Keys).Concat(TypedSinks.Keys).Where(sink => !sinks.Contains(sink)).ToList();

        // Assert
        Assert.True(
            staleSinks.Count == 0,
            $"These listed sinks are no longer in the source:{Environment.NewLine}{string.Join(Environment.NewLine, staleSinks)}");
    }

    [Fact]
    public void Should_name_a_test_that_exists_for_every_covered_sink()
    {
        // Arrange
        var root = RepositoryFolder.Find();

        // Act
        var missingTests = CoveredSinks.Values.Distinct(StringComparer.Ordinal).Where(test => !HasTest(root, test)).ToList();

        // Assert
        Assert.True(
            missingTests.Count == 0,
            $"These covering tests aren't in the files they name:{Environment.NewLine}{string.Join(Environment.NewLine, missingTests)}");
    }

    // Each sink as its file, relative to the repository in forward slashes, and what it writes.
    private static IEnumerable<string> FindSinks()
    {
        var root = RepositoryFolder.Find();
        var sources = Path.Combine(root, "src");

        foreach (var file in EnumerateSourceFiles(sources, "*.razor"))
        {
            var text = File.ReadAllText(file);

            foreach (Match match in BoundAddress().Matches(text))
            {
                yield return $"{GetRelativePath(root, file)} | {match.Groups["attribute"].Value}=@{match.Groups["expression"].Value}";
            }

            foreach (Match match in RazorMarkup().Matches(text))
            {
                yield return $"{GetRelativePath(root, file)} | (MarkupString){match.Groups["expression"].Value}";
            }

            // A root's style is the component's own declarations, found where its builder adds them, and the host's Style.
            foreach (Match match in BoundStyle().Matches(text).Where(match => match.Groups["expression"].Value != "RootStyle"))
            {
                yield return $"{GetRelativePath(root, file)} | style=@{match.Groups["expression"].Value}";
            }
        }

        foreach (var file in EnumerateSourceFiles(sources, "*.cs"))
        {
            foreach (var line in File.ReadLines(file).Select(line => line.Trim()).Where(line => CodeMarkup().IsMatch(line) || CodeStyle().IsMatch(line)))
            {
                yield return $"{GetRelativePath(root, file)} | {line}";
            }
        }

        foreach (var file in EnumerateSourceFiles(sources, "*.js"))
        {
            foreach (var line in File.ReadLines(file).Select(line => line.Trim()).Where(line => !IsComment(line)))
            {
                foreach (Match match in ScriptSink().Matches(line).Concat(ScriptStyle().Matches(line)))
                {
                    yield return $"{GetRelativePath(root, file)} | {match.Value.TrimEnd(';', ' ')}";
                }
            }
        }
    }

    // The library's own source, leaving out the Showcase, which no host receives, and every build's output.
    private static IEnumerable<string> EnumerateSourceFiles(string sources, string pattern)
        => Directory.EnumerateFiles(sources, pattern, SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(folder => folder is "bin" or "obj" or "node_modules" or "Forma.Blazor.Showcase"))
            .Order(StringComparer.Ordinal);

    private static string GetRelativePath(string root, string file)
        => Path.GetRelativePath(root, file).Replace('\\', '/');

    private static bool IsComment(string line)
        => line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('*', StringComparison.Ordinal) || line.StartsWith("/*", StringComparison.Ordinal);

    // A test named as its file and its name stands in that file under that name.
    private static bool HasTest(string root, string test)
    {
        var parts = test.Split(" | ", 2);

        return parts.Length == 2
            && File.Exists(Path.Combine(root, parts[0]))
            && File.ReadAllText(Path.Combine(root, parts[0])).Contains(parts[1], StringComparison.Ordinal);
    }

    // An address attribute whose value is data, from its @ to its closing quote, which a quote inside an indexer doesn't end.
    [GeneratedRegex("""(?<![\w:-])(?<attribute>href|src|srcset|poster|action|formaction|data|xlink:href|srcdoc)\s*=\s*(?:"@(?<expression>.*?)"(?=[\s/>])|@(?<expression>[\w.]+))""", RegexOptions.Singleline)]
    private static partial Regex BoundAddress();

    [GeneratedRegex(@"\(MarkupString\)(?<expression>[^\r\n]*)\)")]
    private static partial Regex RazorMarkup();

    [GeneratedRegex(@"MarkupString\(|\(MarkupString\)|AddMarkupContent\(|AddAttribute\([^)]*""(href|src|srcset|poster|action|formaction|data|srcdoc)""")]
    private static partial Regex CodeMarkup();

    // A style attribute whose value is data, from its @ to its closing quote, an expression in parentheses taken whole.
    [GeneratedRegex("""(?<![\w:-])style\s*=\s*"@(?<expression>\((?>[^()]+|\((?<open>)|\)(?<-open>))*(?(open)(?!))\)|[^"]*)"(?=[\s/>])""", RegexOptions.Singleline)]
    private static partial Regex BoundStyle();

    // A style a render tree writes, and declarations a style builder adds as CSS text.
    [GeneratedRegex(@"AddAttribute\([^)]*""style""|\.AddDeclarations\(")]
    private static partial Regex CodeStyle();

    // A style a script sets through the element's declarations, or writes as an attribute, to the end of its statement's line,
    // since the text it sets can hold a semicolon of its own.
    [GeneratedRegex(@"[\w.#$\[\]]*\.style\.(?:setProperty\(|cssText\s*=(?!=)|[a-zA-Z]+\s*=(?!=)).*|setAttribute\(\s*['""]style['""].*")]
    private static partial Regex ScriptStyle();

    // Markup a script parses or writes into a document, and an address it sets, loads or navigates to.
    [GeneratedRegex(@"[\w.#$\[\]]*\.(innerHTML|outerHTML|srcdoc|src|href|poster|srcset|action|formAction)\s*\+?=(?!=)[^;]*|[\w.]*\.(insertAdjacentHTML|createContextualFragment|parseFromString)\([^;]*|\w*[Dd]ocument\.(write|writeln)\([^;]*|setAttribute\(\s*['""](href|src|srcset|poster|action|formaction|data|srcdoc|xlink:href)['""][^;]*|\bimport\(\s*[^'""`\s)][^)]*\)|location\.(assign|replace)\([^;]*|window\.open\([^;]*")]
    private static partial Regex ScriptSink();
}
