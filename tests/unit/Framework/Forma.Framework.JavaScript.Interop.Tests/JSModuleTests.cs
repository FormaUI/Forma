namespace Forma.Framework.JavaScript.Interop.Tests;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Xunit;

public sealed class JSModuleTests
{
    private const string Path = "./_content/forma/browser/scripts/operations/Probe.js";

    private static readonly Guid Owner = Guid.CreateVersion7();

    private static readonly JsonTypeInfo<int> Int32Type = (JsonTypeInfo<int>)JsonSerializerOptions.Web.GetTypeInfo(typeof(int));

    private static readonly JsonTypeInfo<Extent> ExtentType = (JsonTypeInfo<Extent>)JsonSerializerOptions.Web.GetTypeInfo(typeof(Extent));

    /// <summary>A value an export answers its caller with.</summary>
    public sealed record Extent(string Name, double Width);

    public sealed class Answers
    {
        [Fact]
        public async Task Should_read_what_an_export_answers_as_its_caller_s_type_without_logging()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => new Extent("column", 120.5));
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var extent = await module.TryInvokeAsync("measure", ExtentType);

            // Assert
            Assert.Equal(new Extent("column", 120.5), extent);
            Assert.Empty(loggerFactory.Entries);
        }

        [Theory]
        [InlineData("""{ "name": "column", "width": 1e400 }""")]
        [InlineData("""{ "name": "column", "width": "Infinity" }""")]
        [InlineData("""{ "name": "column", "width": "NaN" }""")]
        [InlineData("""{ "name": "column", "width": "wide" }""")]
        [InlineData("""[1, 2]""")]
        public async Task Should_refuse_an_answer_that_is_not_its_caller_s_type_and_log_it_at_debug(string json)
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => InteropAnswer.Parse(json));
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var extent = await module.TryInvokeAsync("measure", ExtentType);

            // Assert
            Assert.Null(extent);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("was refused", entry.Message, StringComparison.Ordinal);
                Assert.Contains("measure", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_read_null_answered_as_nothing_without_logging()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => null);
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var extent = await module.TryInvokeAsync("measure", ExtentType);

            // Assert
            Assert.Null(extent);
            Assert.Empty(loggerFactory.Entries);
        }

        [Fact]
        public async Task Should_read_a_value_answered_through_the_transfer_as_its_caller_s_type()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => InlineTransfer.Of(new Extent("column", 120.5)));
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var extent = await module.TryInvokeJsonAsync("measure", ExtentType, JSTransferOptions.Default.MaximumLength);

            // Assert
            Assert.Equal(new Extent("column", 120.5), extent);
        }

        [Fact]
        public async Task Should_refuse_a_value_answered_through_the_transfer_that_is_not_its_caller_s_type_and_log_it_at_debug()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => new JSTransfer { Value = InteropAnswer.Parse("""{ "name": "column", "width": "Infinity" }""") });
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var extent = await module.TryInvokeJsonAsync("measure", ExtentType, JSTransferOptions.Default.MaximumLength);

            // Assert
            Assert.Null(extent);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal(LogLevel.Debug, entry.Level),
                () => Assert.Contains("was refused", entry.Message, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Should_log_a_text_longer_than_its_caller_accepts_as_refused_and_a_broken_one_as_lost()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var brokenStream = new FakeJSStreamReference(Encoding.UTF8.GetBytes("text"), new IOException("The stream broke on its way."));
            await using var moduleReference = new FakeJSObjectReference((identifier, _) => identifier == "long" ? InlineTransfer.Of("a long text") : new JSTransfer { Stream = brokenStream, IsText = true });
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var longText = await module.TryInvokeTextAsync("long", maximumLength: 4);
            var brokenText = await module.TryInvokeTextAsync("broken", maximumLength: 1024);

            // Assert
            Assert.Multiple(
                () => Assert.Null(longText),
                () => Assert.Null(brokenText),
                () => Assert.Collection(
                    loggerFactory.Entries,
                    entry => Assert.Contains("was refused", entry.Message, StringComparison.Ordinal),
                    entry => Assert.Contains("was lost", entry.Message, StringComparison.Ordinal)));
        }
    }

    public sealed class TryInvoke
    {
        [Fact]
        public async Task Should_report_a_call_that_ran_without_logging()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => null);
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var ran = await module.TryInvokeVoidAsync("measure");

            // Assert
            Assert.True(ran);
            Assert.Empty(loggerFactory.Entries);
        }

        [Fact]
        public async Task Should_log_a_module_that_could_not_be_imported_without_naming_the_export()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var js = FakeJSRuntime.Throwing(new JSException("Failed to fetch dynamically imported module"));
            await using var module = new JSModule(CreateEnvironment(js, loggerFactory), Path);

            // Act
            var ran = await module.TryInvokeVoidAsync("measure");

            // Assert
            Assert.False(ran);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("could not be imported", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
                Assert.DoesNotContain("measure", entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_import_again_once_an_import_failed_and_log_each_failure_once()
        {
            // Arrange — a module that could not be fetched once
            using var loggerFactory = new FakeLoggerFactory();
            var imports = 0;
            await using var moduleReference = new FakeJSObjectReference((_, _) => null);
            var js = new FakeJSRuntime((_, _) => ++imports == 1 ? throw new JSException("Failed to fetch dynamically imported module") : moduleReference);
            await using var module = new JSModule(CreateEnvironment(js, loggerFactory), Path);
            Assert.False(await module.TryInvokeVoidAsync("measure"));

            // Act
            var ran = await module.TryInvokeVoidAsync("measure");

            // Assert
            Assert.True(ran);
            Assert.Equal(2, imports);
            Assert.Contains("could not be imported", Assert.Single(loggerFactory.Entries).Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Should_log_the_circuit_dropping_during_the_import()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var js = FakeJSRuntime.Throwing(new JSDisconnectedException("The circuit is gone."));
            await using var module = new JSModule(CreateEnvironment(js, loggerFactory), Path);

            // Act
            var value = await module.TryInvokeAsync("measure", Int32Type);

            // Assert
            Assert.Equal(0, value);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("circuit dropped while importing", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_log_the_circuit_dropping_during_the_call()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => throw new JSDisconnectedException("The circuit is gone."));
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var ran = await module.TryInvokeVoidAsync("measure");

            // Assert
            Assert.False(ran);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("circuit dropped while calling", entry.Message, StringComparison.Ordinal);
                Assert.Contains("measure", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_log_a_call_the_runtime_canceled()
        {
            // Arrange — as the runtime cancels a call when its circuit ends or the call runs out of time
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => throw new TaskCanceledException());
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var ran = await module.TryInvokeVoidAsync("measure");

            // Assert
            Assert.False(ran);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("call to measure", entry.Message, StringComparison.Ordinal);
                Assert.Contains("was canceled", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_log_an_import_the_runtime_canceled()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Throwing(new TaskCanceledException()), loggerFactory), Path);

            // Act
            var value = await module.TryInvokeAsync("measure", Int32Type);

            // Assert
            Assert.Equal(0, value);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("Importing", entry.Message, StringComparison.Ordinal);
                Assert.Contains("was canceled", entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_log_a_call_made_once_its_module_was_released()
        {
            // Arrange — as a disposal's release overtakes a call that shared the import still under way
            using var loggerFactory = new FakeLoggerFactory();
            FakeJSObjectReference? moduleReference = null;
            moduleReference = new FakeJSObjectReference((_, _) => moduleReference!.IsDisposed ? throw new ObjectDisposedException(nameof(IJSObjectReference)) : null);
            await moduleReference.DisposeAsync();
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var ran = await module.TryInvokeVoidAsync("attach");

            // Assert
            Assert.False(ran);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("attach in", entry.Message, StringComparison.Ordinal);
                Assert.Contains("wasn't called", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_log_the_export_that_threw()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => throw new JSException("measure is not a function"));
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var value = await module.TryInvokeAsync("measure", Int32Type);

            // Assert
            Assert.Equal(0, value);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("threw", entry.Message, StringComparison.Ordinal);
                Assert.Contains("measure", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }
    }

    public sealed class Invoke
    {
        [Fact]
        public async Task Should_rethrow_the_dropped_circuit_naming_the_module_without_logging()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => throw new JSDisconnectedException("The circuit is gone."));
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var exception = await Assert.ThrowsAsync<JSDisconnectedException>(async () => await module.InvokeVoidAsync("measure"));

            // Assert
            Assert.Contains(Path, exception.Message, StringComparison.Ordinal);
            Assert.Empty(loggerFactory.Entries);
        }
    }

    public sealed class Disposal
    {
        [Fact]
        public async Task Should_release_the_imported_module_without_logging()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => null);
            var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);
            await module.TryInvokeVoidAsync("measure");

            // Act
            await module.DisposeAsync();

            // Assert
            Assert.True(moduleReference.IsDisposed);
            Assert.Empty(loggerFactory.Entries);
        }

        [Fact]
        public async Task Should_release_a_module_whose_import_was_under_way_once_it_lands_without_its_disposal_waiting_for_it()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => null);
            var js = new RecordingJSRuntime((_, _) => moduleReference);
            js.Recorder.Hold("import");
            var module = new JSModule(CreateEnvironment(js, loggerFactory), Path);
            var import = module.ImportAsync();

            // Act
            var disposal = module.DisposeAsync();
            var isDisposedAtOnce = disposal.IsCompleted;
            Assert.Single(js.Recorder.GetCalls("import")).Complete(moduleReference);
            await import;
            await moduleReference.DisposalStarted.WaitAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.True(isDisposedAtOnce),
                () => Assert.True(moduleReference.IsDisposed),
                () => Assert.Empty(loggerFactory.Entries));
        }

        [Fact]
        public async Task Should_not_report_a_failed_import_a_second_time()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var js = FakeJSRuntime.Throwing(new JSException("Failed to fetch dynamically imported module"));
            var module = new JSModule(CreateEnvironment(js, loggerFactory), Path);
            await module.TryInvokeVoidAsync("measure");

            // Act
            await module.DisposeAsync();

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Contains("could not be imported", entry.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Should_log_a_release_the_runtime_canceled()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => null, releaseException: new TaskCanceledException());
            var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);
            await module.TryInvokeVoidAsync("measure");

            // Act
            await module.DisposeAsync();

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("Releasing", entry.Message, StringComparison.Ordinal);
                Assert.Contains("was canceled", entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_log_the_circuit_dropping_during_the_release()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => null, releaseException: new JSDisconnectedException("The circuit is gone."));
            var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);
            await module.TryInvokeVoidAsync("measure");

            // Act
            await module.DisposeAsync();

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("circuit dropped while releasing", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }
    }

    private static JSEnvironment CreateEnvironment(IJSRuntime js, FakeLoggerFactory loggerFactory)
        => new(js, loggerFactory.CreateLogger("probe"), Owner);
}
