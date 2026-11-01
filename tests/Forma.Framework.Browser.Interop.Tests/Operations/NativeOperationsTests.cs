namespace Forma.Framework.Browser.Interop.Tests.Operations;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Browser;
using Forma.Tests.Interop;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class NativeOperationsTests
{
    // A browser whose every module answers each call through one function.
    private static (JSEnvironment Environment, RecordingJSObjectReference Module) CreateBrowser(Func<string, object?[], object?>? respond = null)
    {
        var module = new RecordingJSObjectReference(respond ?? ((_, _) => null));
        var runtime = new RecordingJSRuntime((identifier, _) => identifier == "import" ? module : null);

        return (new JSEnvironment(runtime, NullLogger.Instance, Guid.CreateVersion7()), module);
    }

    public sealed class Saving
    {
        [Fact]
        public async Task Should_hand_the_file_to_the_host_and_save_nothing_in_the_web_view()
        {
            // Arrange
            var (environment, module) = CreateBrowser();
            var native = new FakeNativeOperations();
            await using var download = new Download(environment, native);

            // Act
            var isSaved = await download.SaveAsync("notes.html", "<p>Notes</p>", "text/html");

            // Assert
            Assert.Multiple(
                () => Assert.True(isSaved),
                () => Assert.Equal([("notes.html", "<p>Notes</p>", "text/html")], native.Saved),
                () => Assert.Empty(module.Recorder.GetCalls("save")));
        }

        [Fact]
        public async Task Should_save_in_the_web_view_a_file_the_host_declines()
        {
            // Arrange
            var (environment, module) = CreateBrowser();
            await using var download = new Download(environment, new FakeNativeOperations(takes: false));

            // Act
            await download.SaveAsync("notes.html", "<p>Notes</p>", "text/html");

            // Assert
            Assert.Single(module.Recorder.GetCalls("save"));
        }
    }

    public sealed class Printing
    {
        [Fact]
        public async Task Should_hand_the_host_the_composed_document_as_a_file_named_for_its_title()
        {
            // Arrange
            var (environment, module) = CreateBrowser((identifier, _) => identifier == "compose" ? "<!DOCTYPE html><html></html>" : null);
            var native = new FakeNativeOperations();
            await using var print = new Print(environment, native);

            // Act
            await print.PrintAsync("<p>Report</p>", "Q3/Q4 Report", "prose");

            // Assert
            Assert.Multiple(
                () => Assert.Equal([("Q3Q4 Report.html", "<!DOCTYPE html><html></html>", "text/html")], native.Saved),
                () => Assert.Empty(module.Recorder.GetCalls("print")));
        }

        [Fact]
        public async Task Should_print_in_the_web_view_a_document_the_host_declines()
        {
            // Arrange
            var (environment, module) = CreateBrowser((identifier, _) => identifier == "compose" ? "<!DOCTYPE html><html></html>" : null);
            await using var print = new Print(environment, new FakeNativeOperations(takes: false));

            // Act
            await print.PrintAsync("<p>Report</p>", "Report", "prose");

            // Assert
            Assert.Single(module.Recorder.GetCalls("print"));
        }

        [Fact]
        public async Task Should_name_the_page_s_file_for_a_document_where_its_title_names_nothing()
        {
            // Arrange
            var (environment, _) = CreateBrowser((identifier, _) => identifier == "composePage" ? "<!DOCTYPE html><html></html>" : null);
            var native = new FakeNativeOperations();
            await using var print = new Print(environment, native);

            // Act
            await print.PrintPageAsync(string.Empty);

            // Assert
            Assert.Equal("document.html", Assert.Single(native.Saved).Name);
        }
    }

    public sealed class Sharing
    {
        [Fact]
        public async Task Should_offer_sharing_where_the_host_shares_without_asking_the_browser()
        {
            // Arrange
            var (environment, module) = CreateBrowser();
            await using var share = new Share(environment, new FakeNativeOperations());

            // Act
            var isAvailable = await share.IsAvailableAsync();

            // Assert
            Assert.Multiple(
                () => Assert.True(isAvailable),
                () => Assert.Empty(module.Recorder.GetCalls("isAvailable")));
        }

        [Fact]
        public async Task Should_share_the_address_through_the_host()
        {
            // Arrange
            var (environment, module) = CreateBrowser();
            var native = new FakeNativeOperations();
            await using var share = new Share(environment, native);

            // Act
            await share.ShareAsync("Report", "https://example.com/report");

            // Assert
            Assert.Multiple(
                () => Assert.Equal([("Report", "https://example.com/report")], native.Shared),
                () => Assert.Empty(module.Recorder.GetCalls("share")));
        }

        [Fact]
        public async Task Should_ask_the_browser_where_the_host_shares_nothing()
        {
            // Arrange
            var (environment, module) = CreateBrowser((identifier, _) => identifier == "isAvailable" ? true : null);
            var native = new FakeNativeOperations(canShare: false);
            await using var share = new Share(environment, native);

            // Act
            var isAvailable = await share.IsAvailableAsync();
            await share.ShareAsync("Report", "https://example.com/report");

            // Assert
            Assert.Multiple(
                () => Assert.True(isAvailable),
                () => Assert.Single(module.Recorder.GetCalls("share")),
                () => Assert.Empty(native.Shared));
        }
    }
}
