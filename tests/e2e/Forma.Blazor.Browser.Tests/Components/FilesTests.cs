namespace Forma.Blazor.Browser.Tests.Components;

using System.Text.RegularExpressions;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The file components' interactions, in every runtime.</summary>
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_mark_a_drag_over_it_and_hand_the_file_dropped_to_the_host(string runtime)
        {
            // Arrange — a file held over the drop zone once its script routes the drop
            var (context, page) = await OpenAsync(host, runtime, "file-picker");
            await using var _ = context;
            var zone = page.Locator("#check .forma-file-picker-drop-zone");
            await WaitForInputRouteAsync(page, "#check .forma-file-picker-drop-zone", "file-picker.drop");
            var transfer = await page.EvaluateHandleAsync("() => { const transfer = new DataTransfer(); transfer.items.add(new File(['Hello'], 'notes.txt', { type: 'text/plain' })); return transfer; }");
            await zone.DispatchEventAsync("dragenter", new { dataTransfer = transfer });
            await zone.DispatchEventAsync("dragover", new { dataTransfer = transfer });
            await Assertions.Expect(zone).ToContainClassAsync("forma-file-picker-dragging");

            // Act
            await zone.DispatchEventAsync("drop", new { dataTransfer = transfer });

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("notes.txt (5 bytes)");
            await Assertions.Expect(zone).Not.ToContainClassAsync("forma-file-picker-dragging");
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

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_zoom_its_image_by_the_wheel_held_with_control_and_centre_the_view_on_it(string runtime)
        {
            // Arrange — the pointer over the image once its wheel script routes the turn
            var (context, page) = await OpenImageAsync(host, runtime);
            await using var _ = context;
            var frame = page.Locator("#image .forma-file-viewer-image-frame");
            await WaitForInputRouteAsync(page, "#image .forma-file-viewer-image-frame", "file-viewer.zoom");
            await frame.HoverAsync();

            // Act
            await page.Keyboard.DownAsync("Control");
            await page.Mouse.WheelAsync(0, -100);
            await page.Keyboard.UpAsync("Control");

            // Assert
            await Assertions.Expect(page.Locator("#image .forma-file-viewer-zoom-level")).Not.ToHaveTextAsync("100%");
            await Assertions.Expect(frame).ToContainClassAsync("zoomed");
            await page.WaitForFunctionAsync(IsCentred, await frame.ElementHandleAsync());
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_pan_its_zoomed_image_by_a_drag(string runtime)
        {
            // Arrange — the image zoomed in and centred
            var (context, page) = await OpenImageAsync(host, runtime);
            await using var _ = context;
            var frame = page.Locator("#image .forma-file-viewer-image-frame");
            await WaitForInputRouteAsync(page, "#image .forma-file-viewer-image-frame", "file-viewer.pan");
            await page.Locator("#image").GetByRole(AriaRole.Button, new() { Name = "Zoom In", Exact = true }).ClickAsync();
            await page.WaitForFunctionAsync(IsCentred, await frame.ElementHandleAsync());
            var start = await frame.EvaluateAsync<double>("frame => frame.scrollLeft");
            var box = (await frame.BoundingBoxAsync())!;
            var x = box.X + (box.Width / 2);
            var y = box.Y + (box.Height / 2);

            // Act
            await page.Mouse.MoveAsync(x, y);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(x - 40, y, new() { Steps = 4 });
            await page.Mouse.UpAsync();

            // Assert
            await page.WaitForFunctionAsync("([frame, start]) => Math.abs(frame.scrollLeft - start - 40) <= 1", new object[] { await frame.ElementHandleAsync(), start });
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_leave_the_wheel_alone_once_its_host_turns_wheel_zoom_off(string runtime)
        {
            // Arrange — wheel zoom turned off once its script routes the turn, which takes the route away
            var (context, page) = await OpenImageAsync(host, runtime);
            await using var _ = context;
            var frame = page.Locator("#image .forma-file-viewer-image-frame");
            await WaitForInputRouteAsync(page, "#image .forma-file-viewer-image-frame", "file-viewer.zoom");
            await page.Locator("#wheel-off").ClickAsync();
            await page.WaitForFunctionAsync("frame => window.formaInputRouter.describe(frame)?.routes.some(route => route.action === 'file-viewer.zoom') !== true", await frame.ElementHandleAsync());
            await frame.HoverAsync();

            // Act
            await page.Keyboard.DownAsync("Control");
            await page.Mouse.WheelAsync(0, -100);
            await page.Keyboard.UpAsync("Control");
            await MakeRoundTripAsync(page);

            // Assert
            await Assertions.Expect(page.Locator("#image .forma-file-viewer-zoom-level")).ToHaveTextAsync("100%");
            await Assertions.Expect(frame).Not.ToContainClassAsync("zoomed");
        }

        // Whether the frame's view sits at the middle of what it can scroll across.
        private const string IsCentred = "frame => frame.scrollWidth > frame.clientWidth && Math.abs(frame.scrollLeft - (frame.scrollWidth - frame.clientWidth) / 2) <= 1";

        // The image the viewer shows, wider than it is tall, is a file the check pages' library serves in every host, and the check acts
        // once it has loaded, so an image the host can't serve fails the check rather than leaving the viewer an empty frame to zoom; a
        // check that fails there ends, giving its runtime's turn to the next.
        private static async Task<(IBrowserContext Context, IPage Page)> OpenImageAsync(BrowserHost host, string runtime)
        {
            var (context, page) = await OpenAsync(host, runtime, "file-viewer");

            try
            {
                await Assertions.Expect(page.Locator("#image .forma-file-viewer-image")).Not.ToHaveJSPropertyAsync("naturalWidth", 0);
            }
            catch
            {
                await context.DisposeAsync();

                throw;
            }

            return (context, page);
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
