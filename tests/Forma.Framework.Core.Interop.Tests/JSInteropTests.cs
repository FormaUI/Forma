namespace Forma.Framework.Core.Interop.Tests;

using Forma.Framework.Core.Interop;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Xunit;

public sealed class JSInteropTests
{
    private const string Path = "./_content/forma/common/scripts/behaviours/Probe.js";

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

    public sealed class Detachment
    {
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

    private sealed class Probe(IJSEnvironment js) : JSInterop(js, Path)
    {
        public ValueTask<bool> AttachAsync(ElementReference target)
            => AttachCoreAsync(target);

        public ValueTask<bool> MeasureAsync()
            => InvokeAsync("measure");
    }
}
