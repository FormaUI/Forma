namespace Forma.Framework.JavaScript.Interop.Tests;

using System.Text.Json;

using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Xunit;

public sealed class JSInteropTests
{
    private const string Path = "./_content/forma/browser/scripts/behaviors/Probe.js";

    private static readonly Guid Owner = Guid.CreateVersion7();

    public sealed class Attachment
    {
        [Fact]
        public async Task Should_report_an_attach_the_import_kept_from_running_and_log_the_import()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var js = FakeJSRuntime.Throwing(new JSDisconnectedException("The circuit is gone."));
            await using var probe = new Probe(CreateEnvironment(js, loggerFactory));

            // Act
            var attached = await probe.AttachAsync(default);

            // Assert
            Assert.False(attached);
            Assert.False(probe.IsAttached);
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
        public async Task Should_report_an_attach_that_threw_and_log_it_by_name()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((identifier, _) => identifier == "attach" ? throw new JSException("target is not an element") : null);
            await using var probe = new Probe(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory));

            // Act
            var attached = await probe.AttachAsync(default);

            // Assert
            Assert.False(attached);
            Assert.False(probe.IsAttached);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("attach", entry.Message, StringComparison.Ordinal);
                Assert.Contains("threw", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_release_the_instance_an_attach_gets_once_the_disposal_overtook_it()
        {
            // Arrange — an attach still waiting on the script as the wrapper is disposed
            await using var moduleReference = new RecordingJSObjectReference((_, _) => null);
            moduleReference.Recorder.Hold("attach");
            await using var probe = new Probe(CreateEnvironment(CreateImporting(moduleReference)));
            var attaching = probe.AttachAsync(default).AsTask();
            await probe.DisposeAsync();
            await using var instance = new RecordingJSObjectReference((_, _) => null);

            // Act
            Assert.Single(moduleReference.Recorder.GetCalls("attach")).Complete(instance);

            // Assert
            Assert.False(await attaching);
            Assert.False(probe.IsAttached);
            Assert.Single(instance.Recorder.GetCalls("dispose"));
            Assert.Equal(1, instance.Releases);
        }

        [Fact]
        public async Task Should_release_the_instance_an_attach_gets_once_a_detach_overtook_it()
        {
            // Arrange — an attach still waiting on the script as the wrapper detaches
            await using var moduleReference = new RecordingJSObjectReference((_, _) => null);
            moduleReference.Recorder.Hold("attach");
            await using var probe = new Probe(CreateEnvironment(CreateImporting(moduleReference)));
            var attaching = probe.AttachAsync(default).AsTask();
            await probe.DetachAsync();
            await using var instance = new RecordingJSObjectReference((_, _) => null);

            // Act
            Assert.Single(moduleReference.Recorder.GetCalls("attach")).Complete(instance);

            // Assert
            Assert.False(await attaching);
            Assert.False(probe.IsAttached);
            Assert.Equal(1, instance.Releases);
        }

        [Fact]
        public async Task Should_keep_the_later_of_two_attaches_and_release_the_earlier_answering_last()
        {
            // Arrange — two attaches in flight, the earlier answering after the later
            await using var moduleReference = new RecordingJSObjectReference((_, _) => null);
            moduleReference.Recorder.Hold("attach");
            await using var probe = new Probe(CreateEnvironment(CreateImporting(moduleReference)));
            var earlier = probe.AttachAsync(default).AsTask();
            var later = probe.AttachAsync(default).AsTask();
            var calls = moduleReference.Recorder.GetCalls("attach");
            await using var earlierInstance = new RecordingJSObjectReference((_, _) => null);
            await using var laterInstance = new RecordingJSObjectReference((_, _) => null);
            calls[1].Complete(laterInstance);

            // Act
            calls[0].Complete(earlierInstance);

            // Assert
            Assert.True(await later);
            Assert.False(await earlier);
            Assert.True(probe.IsAttached);
            Assert.Equal(1, earlierInstance.Releases);
            Assert.Equal(0, laterInstance.Releases);
        }

        [Fact]
        public async Task Should_attach_on_a_later_attempt_once_the_import_failed()
        {
            // Arrange — a module that could not be fetched once
            var imports = 0;
            await using var moduleReference = new FakeJSObjectReference((identifier, _) => identifier == "attach" ? new FakeJSObjectReference((_, _) => null) : null);
            var js = new FakeJSRuntime((_, _) => ++imports == 1 ? throw new JSException("Failed to fetch dynamically imported module") : moduleReference);
            await using var probe = new Probe(CreateEnvironment(js));
            Assert.False(await probe.AttachAsync(default));

            // Act
            var attached = await probe.AttachAsync(default);

            // Assert
            Assert.True(attached);
            Assert.True(probe.IsAttached);
        }
    }

    public sealed class Calls
    {
        [Fact]
        public async Task Should_report_a_call_before_attaching_without_logging()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => null);
            await using var probe = new Probe(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory));

            // Act
            var ran = await probe.MeasureAsync();

            // Assert
            Assert.False(ran);
            Assert.Empty(loggerFactory.Entries);
        }

        [Fact]
        public async Task Should_log_a_call_the_attached_instance_threw_at_debug()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var handle = new FakeJSObjectReference((identifier, _) => identifier == "measure" ? throw new JSException("measure is not a function") : null);
            await using var moduleReference = new FakeJSObjectReference((identifier, _) => identifier == "attach" ? handle : null);
            await using var probe = new Probe(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory));
            await probe.AttachAsync(default);

            // Act
            var ran = await probe.MeasureAsync();

            // Assert
            Assert.False(ran);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("measure", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }
    }

    public sealed class Answers
    {
        [Fact]
        public async Task Should_read_a_value_the_attached_instance_answers_inline_through_the_transfer()
        {
            // Arrange
            await using var handle = new FakeJSObjectReference((identifier, _) => identifier == "read" ? InlineTransfer.Of(new { name = "Ada" }) : null);
            await using var moduleReference = new FakeJSObjectReference((identifier, _) => identifier == "attach" ? handle : null);
            await using var probe = new Probe(CreateEnvironment(FakeJSRuntime.Importing(moduleReference)));
            await probe.AttachAsync(default);

            // Act
            var value = await probe.ReadAsync();

            // Assert
            Assert.Equal("Ada", value?.GetProperty("name").GetString());
        }

        [Fact]
        public async Task Should_read_a_value_the_attached_instance_streams_past_the_budget_and_release_its_stream()
        {
            // Arrange
            var name = new string('a', 40_000);
            await using var stream = new FakeJSStreamReference(JsonSerializer.SerializeToUtf8Bytes(new { name }));
            await using var handle = new FakeJSObjectReference((identifier, _) => identifier == "read" ? new JSTransfer { Stream = stream } : null);
            await using var moduleReference = new FakeJSObjectReference((identifier, _) => identifier == "attach" ? handle : null);
            await using var probe = new Probe(CreateEnvironment(FakeJSRuntime.Importing(moduleReference)));
            await probe.AttachAsync(default);

            // Act
            var value = await probe.ReadAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(name, value?.GetProperty("name").GetString()),
                () => Assert.True(stream.IsDisposed));
        }

        [Fact]
        public async Task Should_answer_nothing_for_a_value_larger_than_the_environment_accepts_and_log_it_at_debug()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var stream = new FakeJSStreamReference([], length: JSTransferOptions.Default.MaximumLength + 1);
            await using var handle = new FakeJSObjectReference((identifier, _) => identifier == "read" ? new JSTransfer { Stream = stream } : null);
            await using var moduleReference = new FakeJSObjectReference((identifier, _) => identifier == "attach" ? handle : null);
            await using var probe = new Probe(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory));
            await probe.AttachAsync(default);

            // Act
            var value = await probe.ReadAsync();

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Null(value),
                () => Assert.True(stream.IsDisposed),
                () => Assert.Equal(LogLevel.Debug, entry.Level),
                () => Assert.Contains("read", entry.Message, StringComparison.Ordinal));
        }
    }

    public sealed class Detachment
    {
        [Fact]
        public async Task Should_log_a_dispose_the_runtime_canceled_at_debug()
        {
            // Arrange — as the runtime cancels the calls of a circuit that ends while its components are disposed
            using var loggerFactory = new FakeLoggerFactory();
            await using var handle = new FakeJSObjectReference((identifier, _) => identifier == "dispose" ? throw new TaskCanceledException() : null);
            await using var moduleReference = new FakeJSObjectReference((identifier, _) => identifier == "attach" ? handle : null);
            await using var probe = new Probe(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory));
            await probe.AttachAsync(default);

            // Act
            await probe.DetachAsync();

            // Assert
            Assert.False(probe.IsAttached);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("call to dispose", entry.Message, StringComparison.Ordinal);
                Assert.Contains("was canceled", entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_log_a_dispose_the_circuit_dropped_under_at_debug()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var handle = new FakeJSObjectReference((identifier, _) => identifier == "dispose" ? throw new JSDisconnectedException("The circuit is gone.") : null);
            await using var moduleReference = new FakeJSObjectReference((identifier, _) => identifier == "attach" ? handle : null);
            await using var probe = new Probe(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory));
            await probe.AttachAsync(default);

            // Act
            await probe.DetachAsync();

            // Assert
            Assert.False(probe.IsAttached);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("circuit dropped", entry.Message, StringComparison.Ordinal);
                Assert.Contains("dispose", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }
    }

    private static JSEnvironment CreateEnvironment(IJSRuntime js, FakeLoggerFactory loggerFactory)
        => new(js, loggerFactory.CreateLogger("probe"), Owner);

    private static JSEnvironment CreateEnvironment(IJSRuntime js)
        => new(js, NullLogger.Instance, Owner);

    private static FakeJSRuntime CreateImporting(IJSObjectReference moduleReference)
        => new((identifier, _) => identifier == "import" ? moduleReference : throw new JSException($"'{identifier}' is not an import."));

    private sealed class Probe(IJSEnvironment js) : JSInterop(js, Path)
    {
        public ValueTask<bool> AttachAsync(ElementReference target)
            => AttachCoreAsync(target);

        public ValueTask<bool> MeasureAsync()
            => InvokeAsync("measure");

        public ValueTask<JsonElement?> ReadAsync()
            => InvokeJsonAsync("read");
    }
}
