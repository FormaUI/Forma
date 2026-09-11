namespace Forma.Framework.Core.Interop.Tests;

using Forma.Framework.Core.Interop;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Xunit;

public sealed class JSModuleTests
{
    private const string Path = "./_content/forma/common/scripts/operations/Probe.js";

    private static readonly Guid Owner = Guid.CreateVersion7();

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
        public async Task Should_log_the_dropped_circuit_at_debug()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var js = FakeJSRuntime.Throwing(new JSDisconnectedException("The circuit is gone."));
            await using var module = new JSModule(CreateEnvironment(js, loggerFactory), Path);

            // Act
            var ran = await module.TryInvokeVoidAsync("measure");

            // Assert
            Assert.False(ran);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("circuit dropped", entry.Message, StringComparison.Ordinal);
                Assert.Contains("measure", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task Should_log_the_thrown_script_at_debug()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var moduleReference = new FakeJSObjectReference((_, _) => throw new JSException("measure is not a function"));
            await using var module = new JSModule(CreateEnvironment(FakeJSRuntime.Importing(moduleReference), loggerFactory), Path);

            // Act
            var value = await module.TryInvokeAsync<int>("measure");

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
        public async Task Should_log_an_import_that_failed_at_debug()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var js = FakeJSRuntime.Throwing(new JSException("Failed to fetch dynamically imported module"));
            var module = new JSModule(CreateEnvironment(js, loggerFactory), Path);
            await module.TryInvokeVoidAsync("measure");

            // Act
            await module.DisposeAsync();

            // Assert
            Assert.Equal(2, loggerFactory.Entries.Count);
            var entry = loggerFactory.Entries[1];
            Assert.Multiple(() =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains("could not be imported", entry.Message, StringComparison.Ordinal);
                Assert.Contains(Path, entry.Message, StringComparison.Ordinal);
                Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
            });
        }
    }

    private static JSEnvironment CreateEnvironment(IJSRuntime js, FakeLoggerFactory loggerFactory)
        => new(js, loggerFactory.CreateLogger("probe"), Owner);
}
