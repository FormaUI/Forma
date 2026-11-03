namespace Forma.Framework.Browser.Interop.Tests.Text;

using Forma.Framework.Browser.Interop.Text;
using Forma.Tests.Browser;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;

using Xunit;

public sealed class DomTextTests
{
    private static readonly ElementReference Field = new("field");

    private static FakeDomBrowser CreateBrowser()
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.EnginePath, "attach") => true,
            _ => null,
        });

    public sealed class Counting
    {
        [Fact]
        public async Task Should_ask_the_browser_to_count_into_the_output_with_the_most_the_field_accepts()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            await using var interaction = await engine.GetElement(Field).Text.AttachAsync(new CharacterCountOptions { OutputId = "field-count", MaxLength = 280 });

            // Assert
            var attach = Assert.Single(browser.Engine.Recorder.GetCalls("attach"));
            Assert.Multiple(
                () => Assert.Equal("text", FakeDomBrowser.GetRequestField(attach, "capability")),
                () => Assert.Equal("field-count", FakeDomBrowser.GetOption(attach, "output")),
                () => Assert.Equal(280, FakeDomBrowser.GetOption(attach, "maxLength")));
        }

        [Fact]
        public async Task Should_make_no_call_while_it_is_not_wanted()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            await using var interaction = await engine.GetElement(Field).Text.AttachAsync(new CharacterCountOptions { OutputId = "field-count" }, shouldEnable: false);

            // Assert
            Assert.Empty(browser.Engine.Recorder.GetCalls("attach"));
        }

        [Fact]
        public async Task Should_refuse_an_empty_output()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => engine.GetElement(Field).Text.AttachAsync(new CharacterCountOptions { OutputId = " " }));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("options", exception.ParamName),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }

        [Fact]
        public async Task Should_refuse_a_limit_below_one()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => engine.GetElement(Field).Text.AttachAsync(new CharacterCountOptions { OutputId = "field-count", MaxLength = 0 }));

            // Assert
            Assert.Equal("options", exception.ParamName);
        }
    }
}
