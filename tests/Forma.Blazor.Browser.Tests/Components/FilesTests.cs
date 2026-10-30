namespace Forma.Blazor.Browser.Tests.Components;

using System.Text.RegularExpressions;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The file components' main interactions, in every runtime.</summary>
public sealed partial class FilesTests
{
    public sealed class FilePicker(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hand_the_file_picked_to_the_host(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "file-picker");
            await using var _ = context;
            var input = page.Locator("#check input[type=file]");
            await WaitForFileInputAsync(page, "#check input[type=file]");

            // Act
            await input.SetInputFilesAsync(CreateTextFile());

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("notes.txt (5 bytes)");
            await Assertions.Expect(page.Locator("#check [role=alert] li")).ToHaveCountAsync(0);
        }
    }

    public sealed class FileUploader(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_store_the_file_picked_and_report_it_stored(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "file-uploader");
            await using var _ = context;
            var input = page.Locator("#picker input[type=file]");
            await WaitForFileInputAsync(page, "#picker input[type=file]");

            // Act
            await input.SetInputFilesAsync(CreateTextFile());

            // Assert
            await Assertions.Expect(page.Locator("#check .forma-file-uploader-state")).ToHaveTextAsync("Stored");
            await Assertions.Expect(page.GetByRole(AriaRole.Progressbar, new() { Name = "notes.txt", Exact = true })).ToHaveAttributeAsync("value", "100");
            await Assertions.Expect(page.Locator("#check [role=status]")).ToHaveTextAsync("Stored 1 of 1.");
            await Assertions.Expect(page.Locator("#check")).ToHaveAttributeAsync("aria-busy", "false");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("notes.txt (5 bytes)");
        }
    }

    public sealed class FileViewer(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_the_document_given_and_step_to_the_next_one(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "file-viewer");
            await using var _ = context;
            var document = page.Locator("#check .forma-file-viewer-html");
            var next = page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true });
            await Assertions.Expect(document.GetByRole(AriaRole.Heading, new() { Name = "Notes", Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(document).ToHaveAttributeAsync("aria-label", "notes.md");

            // Act
            await next.ClickAsync();

            // Assert
            await Assertions.Expect(document.GetByRole(AriaRole.Heading, new() { Name = "To Do", Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(document).ToHaveAttributeAsync("aria-label", "todo.md");
            await Assertions.Expect(next).ToBeDisabledAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Previous", Exact = true })).ToBeEnabledAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("check:todo.md");
        }
    }

    public sealed class ImagePicker(BrowserHost host)
    {
        // A PNG of one grey pixel.
        private static readonly byte[] PixelImage = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_take_the_image_picked_and_show_its_thumbnail(string runtime)
        {
            // Arrange — the picker's file input ready, its thumbnails' script left to attach in its own time
            var (context, page) = await OpenAsync(host, runtime, "image-picker");
            await using var _ = context;
            var input = page.Locator("#check input[type=file]");
            await WaitForFileInputAsync(page, "#check input[type=file]");

            // Act
            await input.SetInputFilesAsync(new FilePayload { Name = "pixel.png", MimeType = "image/png", Buffer = PixelImage });

            // Assert
            await Assertions.Expect(page.Locator("#check .forma-image-picker-name")).ToHaveTextAsync("pixel.png");
            await Assertions.Expect(page.Locator("#check img[data-forma-preview]")).ToHaveAttributeAsync("src", ObjectAddressPattern());
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("pixel.png (68 bytes)");
        }
    }

    private static FilePayload CreateTextFile()
        => new() { Name = "notes.txt", MimeType = "text/plain", Buffer = "Hello"u8.ToArray() };

    [GeneratedRegex("^blob:")]
    private static partial Regex ObjectAddressPattern();
}
