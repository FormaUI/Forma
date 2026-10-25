namespace Forma.Framework.Globalization.Tests;

using System.Globalization;

using Forma.Tests.Shared;

using Xunit;

public sealed class AmbientCultureTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    private static string GetAmbientCultures()
        => $"{CultureInfo.CurrentCulture.Name}/{CultureInfo.CurrentUICulture.Name}";

    private static Exception? RecordRun<TResult>(Func<TResult> operation)
        => Record.Exception(() => { AmbientCulture.Run(English, English, operation); });

    public sealed class Operation
    {
        [Fact]
        public void Should_run_the_operation_in_the_given_cultures()
        {
            // Arrange
            using var fixture = new CultureFixture("en");

            // Act
            var ambientCultures = AmbientCulture.Run(German, Hebrew, GetAmbientCultures);

            // Assert
            Assert.Equal("de-DE/he", ambientCultures);
        }

        [Fact]
        public void Should_restore_the_callers_cultures_afterward()
        {
            // Arrange
            using var fixture = new CultureFixture("en");

            // Act
            AmbientCulture.Run(German, Hebrew, GetAmbientCultures);

            // Assert
            Assert.Equal("en/en", GetAmbientCultures());
        }

        [Fact]
        public void Should_restore_the_callers_cultures_when_the_operation_throws()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var failure = new InvalidOperationException("The operation failed.");

            // Act
            var exception = Record.Exception(() => AmbientCulture.Run<string>(German, Hebrew, () => throw failure));

            // Assert
            Assert.Multiple(
                () => Assert.Same(failure, exception),
                () => Assert.Equal("en/en", GetAmbientCultures()));
        }

        [Fact]
        public void Should_restore_the_callers_cultures_when_the_operation_changes_them()
        {
            // Arrange
            using var fixture = new CultureFixture("en");

            // Act
            AmbientCulture.Run(German, Hebrew, () =>
            {
                CultureInfo.CurrentCulture = Hebrew;
                CultureInfo.CurrentUICulture = German;
            });

            // Assert
            Assert.Equal("en/en", GetAmbientCultures());
        }

        [Fact]
        public void Should_unwind_nested_operations_in_order()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var ambientCultures = new List<string>();

            // Act
            AmbientCulture.Run(German, German, () =>
            {
                ambientCultures.Add(GetAmbientCultures());
                ambientCultures.Add(AmbientCulture.Run(Hebrew, Hebrew, GetAmbientCultures));
                ambientCultures.Add(GetAmbientCultures());
            });
            ambientCultures.Add(GetAmbientCultures());

            // Assert
            Assert.Equal(["de-DE/de-DE", "he/he", "de-DE/de-DE", "en/en"], ambientCultures);
        }

        [Fact]
        public void Should_run_an_action_in_the_given_cultures()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var ambientCultures = "";

            // Act
            AmbientCulture.Run(German, Hebrew, () => { ambientCultures = GetAmbientCultures(); });

            // Assert
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", ambientCultures),
                () => Assert.Equal("en/en", GetAmbientCultures()));
        }

        [Fact]
        public void Should_refuse_an_operation_that_returns_a_task()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var runs = 0;

            // Act
            var exceptions = new[]
            {
                RecordRun(() => { runs++; return Task.CompletedTask; }),
                RecordRun(() => { runs++; return Task.FromResult(1); }),
                RecordRun(() => { runs++; return ValueTask.CompletedTask; }),
                RecordRun(() => { runs++; return ValueTask.FromResult(1); }),
            };

            // Assert
            Assert.Multiple(
                () => Assert.All(exceptions, exception => Assert.IsType<ArgumentException>(exception)),
                () => Assert.Equal(0, runs));
        }
    }
}
