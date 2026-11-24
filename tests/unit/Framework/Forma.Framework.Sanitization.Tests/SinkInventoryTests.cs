namespace Forma.Framework.Sanitization.Tests;

using System.Text.RegularExpressions;

using Forma.Tests.Shared;

using Xunit;

/// <summary>
/// Every place the library writes markup or an address into a page, found from its source: an address attribute bound to
/// data or a <c>MarkupString</c> in a component, and markup a script parses or writes or an address it sets, loads or
/// navigates to. Each is covered by a test giving it hostile input, or writes what no data reaches.
/// </summary>
public sealed partial class SinkInventoryTests
{
    private const string BlazorTests = "tests/bunit/Blazor/Forma.Blazor.BUnit.Tests/Components";
    private const string ComposerTests = "tests/bunit/Blazor/Forma.Blazor.Composer.BUnit.Tests";
    private const string ExplorerTests = "tests/bunit/Blazor/Forma.Blazor.Explorer.BUnit.Tests/FormaExplorerTests.razor";
    private const string Components = "src/Blazor/Forma.Blazor/Components";
    private const string Composer = "src/Blazor/Forma.Blazor.Composer";
    private const string BrowserScripts = "src/Framework/Forma.Framework.Browser.Interop/wwwroot/scripts";

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
        [$"{Composer}/wwwroot/scripts/RichTextEditor/Canvas.mjs | this.#element.innerHTML = value"] = $"{ComposerTests}/RichTextEditor/FormaRichTextEditorTests.razor | Should_write_a_template_into_the_canvas_only_as_the_sanitizer_keeps_it",
        [$"{Composer}/wwwroot/scripts/RichTextEditor/ContentWriter.mjs | template.innerHTML = html"] = $"{ComposerTests}/RichTextEditor/FormaRichTextEditorTests.razor | Should_write_a_template_into_the_canvas_only_as_the_sanitizer_keeps_it",
        ["src/Blazor/Forma.Blazor.Explorer/FormaExplorerThumbnail.razor | src=@still"] = $"{ExplorerTests} | Should_draw_its_glyph_for_a_still_at_an_address_an_image_refuses",
        ["src/Blazor/Forma.Blazor.Explorer/FormaExplorerThumbnail.razor | src=@frame"] = $"{ExplorerTests} | Should_draw_no_frame_of_a_video_at_an_address_a_video_refuses",
        [$"{BrowserScripts}/operations/Asset.js | .parseFromString(await response.text(), 'image/svg+xml')"] = $"{BlazorTests}/Symbols/FormaEmojiTests.razor | Should_draw_an_emoji_whose_sheet_an_image_refuses_as_its_own_character_and_load_no_sheet",
        [$"{BrowserScripts}/operations/Print.js | article.innerHTML = html"] = $"{ComposerTests}/FormaComposerTests.razor | Should_preview_and_print_a_hostile_document_only_as_the_sanitizer_keeps_it",
        [$"{BrowserScripts}/operations/Print.js | printedDocument.body.firstElementChild.innerHTML = html"] = $"{ComposerTests}/FormaComposerTests.razor | Should_preview_and_print_a_hostile_document_only_as_the_sanitizer_keeps_it",
        ["src/Framework/Forma.Framework.JavaScript.Interop/wwwroot/scripts/Sanitizer.js | template.innerHTML = html ?? ''"] = "tests/scripts/core/Sanitizer.test.js | drops a script and an image error handler from markup",
    };

    // The sinks no data reaches, each with what it writes.
    private static readonly Dictionary<string, string> UnreachedSinks = new(StringComparer.Ordinal)
    {
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@DocumentBase"] = "The page's base address, which the host configures",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@IconHref"] = "The page's icon, which the host configures",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[\"_content/Forma.Blazor/forma-tokens.css\"]"] = "The library's own stylesheet",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[\"_content/Forma.Blazor/forma-reset.css\"]"] = "The library's own stylesheet",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[\"_content/Forma.Blazor/forma-base.css\"]"] = "The library's own stylesheet",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[\"_content/Forma.Blazor/forma-input.css\"]"] = "The library's own stylesheet",
        ["src/Blazor/Forma.Blazor.Server/Components/Page/FormaPageHead.razor | href=@Assets[\"_content/Forma.Blazor/forma-prose.css\"]"] = "The library's own stylesheet",
        [$"{BrowserScripts}/behaviors/FilePreview.js | image.src = url"] = "A blob address the script makes of a file the reader picked",
        [$"{BrowserScripts}/operations/Download.js | link.href = url"] = "A blob address the script makes of the text it saves",
        [$"{BrowserScripts}/operations/Asset.js | import(url)"] = "A module a composer plugin declares in its code",
        [$"{BrowserScripts}/operations/Asset.js | script.src = url"] = "A script a player provider declares in its code",
        [$"{BrowserScripts}/operations/Asset.js | link.href = url"] = "A stylesheet a composer plugin declares in its code",
        [$"{BrowserScripts}/operations/Print.js | printedDocument.write(`<!DOCTYPE html><html lang=\"${{document.documentElement.lang}}\" dir=\"${{document.documentElement.dir}}\"><head><meta charset=\"utf-8\"><title></title></head><body><article></article></body></html>`)"] = "The library's own document, in the page's own language and direction",
        [$"{BrowserScripts}/operations/Print.js | link.href = href"] = "The page's own stylesheets",
        ["src/Framework/Forma.Framework.Culture.Browser.Interop/wwwroot/scripts/CultureCoordinator.js | location.assign(destination)"] = "The page's own address in another culture",
    };

    [Fact]
    public void Should_cover_every_sink_the_source_writes()
    {
        // Act
        var uncoveredSinks = FindSinks()
            .Where(sink => !CoveredSinks.ContainsKey(sink) && !UnreachedSinks.ContainsKey(sink))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // Assert
        Assert.True(
            uncoveredSinks.Count == 0,
            $"These sinks write markup or an address with no test giving them hostile input:{Environment.NewLine}{string.Join(Environment.NewLine, uncoveredSinks)}");
    }

    [Fact]
    public void Should_list_only_sinks_the_source_still_writes()
    {
        // Arrange
        var sinks = FindSinks().ToHashSet(StringComparer.Ordinal);

        // Act
        var staleSinks = CoveredSinks.Keys.Concat(UnreachedSinks.Keys).Where(sink => !sinks.Contains(sink)).ToList();

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
        }

        foreach (var file in EnumerateSourceFiles(sources, "*.cs"))
        {
            foreach (var line in File.ReadLines(file).Select(line => line.Trim()).Where(line => CodeMarkup().IsMatch(line)))
            {
                yield return $"{GetRelativePath(root, file)} | {line}";
            }
        }

        foreach (var file in EnumerateSourceFiles(sources, "*.js").Concat(EnumerateSourceFiles(sources, "*.mjs")))
        {
            foreach (var line in File.ReadLines(file).Select(line => line.Trim()).Where(line => !IsComment(line)))
            {
                foreach (Match match in ScriptSink().Matches(line))
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

    // Markup a script parses or writes into a document, and an address it sets, loads or navigates to.
    [GeneratedRegex(@"[\w.#$\[\]]*\.(innerHTML|outerHTML|srcdoc|src|href|poster|srcset|action|formAction)\s*\+?=(?!=)[^;]*|[\w.]*\.(insertAdjacentHTML|createContextualFragment|parseFromString)\([^;]*|\w*[Dd]ocument\.(write|writeln)\([^;]*|setAttribute\(\s*['""](href|src|srcset|poster|action|formaction|data|srcdoc|xlink:href)['""][^;]*|\bimport\(\s*[^'""`\s)][^)]*\)|location\.(assign|replace)\([^;]*|window\.open\([^;]*")]
    private static partial Regex ScriptSink();
}
