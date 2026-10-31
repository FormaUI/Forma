namespace Forma.Framework.JavaScript.Interop.Tests;

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
            var value = await module.TryInvokeAsync<int>("measure");

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
        public async Task Should_log_the_export_that_threw()
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
