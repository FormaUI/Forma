namespace Forma.Framework.Culture.Browser.Interop.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

public sealed class CultureCoordinatorRegistrationTests
{
    private static ServiceProvider CreateServices(FakeCultureCoordinatorBrowser browser)
        => new ServiceCollection()
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddFormaCultureCoordination()
            .BuildServiceProvider();

    private static CultureCoordinatorRegistration GetRegistration(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<CultureCoordinatorRegistration>();

    private static ICultureState GetCultureState(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<ICultureState>();

    public sealed class Registering
    {
        [Fact]
        public async Task Should_adopt_the_pair_the_page_accepted()
        {
            // Arrange — a scope that starts in English, on a page the server rendered in Hebrew text with German formats
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser("de-DE", "he");
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            await GetRegistration(scope).RegisterAsync();

            // Assert
            var state = GetCultureState(scope);
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Equal(browser.DocumentKey, state.Version.DocumentKey),
                () => Assert.Equal(0, state.Version.Number));
        }

        [Fact]
        public async Task Should_register_again_under_its_identity_as_each_connection_comes_up()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            await registration.RegisterAsync();

            // Act
            registration.MarkUnavailable();
            await registration.RegisterAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(2, browser.CountRegistrations()),
                () => Assert.Single(browser.Identities));
        }

        [Fact]
        public async Task Should_adopt_the_pair_the_page_accepted_meanwhile_as_it_registers_again()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            await registration.RegisterAsync();
            registration.MarkUnavailable();
            browser.CloseCircuit();
            await browser.LandAsync("he", "he");

            // Act
            await registration.RegisterAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Equal("he", GetCultureState(scope).UICulture.Name),
                () => Assert.Equal(1, GetCultureState(scope).Version.Number));
        }

        [Fact]
        public async Task Should_fail_where_the_page_can_not_coordinate()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            browser.FailNext("register", new JSException("The page carries no valid culture marker."));
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            var exception = await Record.ExceptionAsync(() => GetRegistration(scope).RegisterAsync());

            // Assert
            Assert.Multiple(
                () => Assert.IsType<JSException>(exception),
                () => Assert.Equal(0, GetCultureState(scope).Revision));
        }
    }

    public sealed class Starting
    {
        [Fact]
        public async Task Should_hand_the_coordinator_the_start_it_is_given_and_none_where_it_is_given_none()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            var start = new CultureCoordinatorStart
            {
                Marker = CulturePublication.GetMarkerAttributes(CultureInfo.GetCultureInfo("he"), CultureInfo.GetCultureInfo("he")),
                Policy = CulturePublication.GetPolicyAttributes(CultureSwitchPolicy.Default, map: null),
                Direction = "rtl",
            };

            // Act
            await registration.RegisterAsync(start);
            registration.MarkUnavailable();
            await registration.RegisterAsync();

            // Assert
            var calls = browser.Module.Recorder.GetCalls("register");
            Assert.Multiple(
                () => Assert.Same(start, calls[0].Arguments[3]),
                () => Assert.Null(calls[1].Arguments[3]));
        }
    }

    public sealed class Following
    {
        [Fact]
        public async Task Should_apply_each_pair_the_page_accepts()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();

            // Act
            await browser.LandAsync("de-DE", "he");

            // Assert
            var state = GetCultureState(scope);
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Equal(1, state.Version.Number));
        }

        [Fact]
        public async Task Should_complete_a_delivery_once_the_state_holds_the_pair_before_its_subscribers_are_notified()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();
            var gate = new TaskCompletionSource();
            GetCultureState(scope).Subscribe(() => gate.Task);

            // Act
            await browser.LandAsync("he", "he");

            // Assert
            Assert.Equal("he", GetCultureState(scope).UICulture.Name);
            gate.SetResult();
        }

        [Fact]
        public async Task Should_log_a_subscriber_that_fails_as_it_is_notified()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = new ServiceCollection()
                .AddSingleton<IJSRuntime>(browser.Runtime)
                .AddSingleton<ILoggerFactory>(loggerFactory)
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .AddFormaCultureCoordination()
                .BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();
            GetCultureState(scope).Subscribe(() => Task.FromException(new InvalidOperationException("A subscriber failed.")));

            // Act — the subscribers hear of the landing once the delivery has returned, and the failure is logged then
            await browser.LandAsync("es-419", "es-419");
            var entry = await loggerFactory.WaitForEntryAsync(written => written.Level == LogLevel.Error);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(typeof(CultureCoordinatorRegistration).FullName, entry.Category),
                () => Assert.DoesNotContain("es-419", entry.Message, StringComparison.Ordinal),
                () => Assert.Single(loggerFactory.Entries));
        }

        [Fact]
        public async Task Should_report_each_landing_with_its_place_among_the_registrations_reports()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser("en", "en");
            browser.UseRoutes("en", ("en", "en", "en"), ("he", "he", "he"));
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            await registration.RegisterAsync();

            // Act
            await registration.ReportLandingAsync("he");
            await registration.ReportLandingAsync(null);

            // Assert
            var reports = browser.GetRegistration(browser.Identities.Single()).Recorder.GetCalls("reportLanding");
            Assert.Multiple(
                () => Assert.Equal([("he", 1), (null, 2)], reports.Select(report => ((string?)report.Arguments[0], (int)report.Arguments[1]!))),
                () => Assert.Equal("en", GetCultureState(scope).UICulture.Name));
        }

        [Fact]
        public async Task Should_leave_the_page_on_the_later_landing_where_the_earlier_report_reaches_it_last()
        {
            // Arrange — the landing at Hebrew waits for a registration that answers after a later one, which the landing at the
            // plain address made after it waits for
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser("en", "en");
            browser.UseRoutes("en", ("en", "en", "en"), ("he", "he", "he"));
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            browser.HoldRegistrations();
            _ = registration.RegisterAsync();
            var hebrew = registration.ReportLandingAsync("he");
            _ = registration.RegisterAsync();
            var plain = registration.ReportLandingAsync(null);

            // Act
            browser.AnswerRegistration(1);
            await plain;
            browser.AnswerRegistration(0);
            await hebrew;

            // Assert
            Assert.Equal("en", GetCultureState(scope).UICulture.Name);
        }

        [Fact]
        public async Task Should_leave_the_document_on_the_later_follow_where_the_earlier_one_reaches_it_last()
        {
            // Arrange — the follow into German waits for a registration that answers after a later one, which the follow into
            // Hebrew made after it waits for
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            var german = CultureInfo.GetCultureInfo("de-DE");
            var hebrew = CultureInfo.GetCultureInfo("he");
            browser.HoldRegistrations();
            _ = registration.RegisterAsync();
            var germanFollow = registration.FollowAsync(german, german, TestContext.Current.CancellationToken);
            _ = registration.RegisterAsync();
            var hebrewFollow = registration.FollowAsync(hebrew, hebrew, TestContext.Current.CancellationToken);

            // Act
            browser.AnswerRegistration(1);
            var isHebrewFollowed = await hebrewFollow;
            browser.AnswerRegistration(0);
            var isGermanFollowed = await germanFollow;

            // Assert
            Assert.Multiple(
                () => Assert.True(isHebrewFollowed && isGermanFollowed),
                () => Assert.Equal([("he", "he")], browser.Followed),
                () => Assert.Equal(("he", "he"), (browser.Accepted.Culture, browser.Accepted.UICulture)));
        }

        [Fact]
        public async Task Should_release_once_disposed_where_the_runtime_canceled_the_release()
        {
            // Arrange — as the runtime cancels the calls of a circuit that ends while its scope is disposed
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            await registration.RegisterAsync();
            browser.FailNext("dispose", new TaskCanceledException());

            // Act
            var disposal = registration.DisposeAsync().AsTask();
            await disposal;

            // Assert
            Assert.True(disposal.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task Should_release_a_registration_under_way_once_it_lands_without_its_disposal_waiting_for_it()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            browser.Module.Recorder.Hold("register");
            var attempt = registration.RegisterAsync();
            await using var reference = new RecordingJSObjectReference((_, _) => null);

            // Act
            var disposal = registration.DisposeAsync();
            var isDisposedAtOnce = disposal.IsCompleted;
            Assert.Single(browser.Module.Recorder.GetCalls("register")).Complete(reference);
            await attempt;
            await reference.DisposalStarted.WaitAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.True(isDisposedAtOnce),
                () => Assert.Single(reference.Recorder.GetCalls("dispose")),
                () => Assert.Equal(1, reference.DisposalCount));
        }

        [Fact]
        public async Task Should_apply_nothing_once_disposed()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            await registration.RegisterAsync();
            var identity = Assert.Single(browser.Identities);
            var reference = browser.GetRegistration(identity);

            // Act
            await registration.DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Empty(browser.Identities),
                () => Assert.Equal(1, reference.DisposalCount),
                () => Assert.Equal(CultureInfo.GetCultureInfo("en-US"), GetCultureState(scope).UICulture));
        }
    }

    public sealed class Provision
    {
        // A state of the host's own, which applies the cultures it is handed as they are.
        private static ServiceProvider CreateServices(FakeCultureCoordinatorBrowser browser, FakeCultureProvider cultureProvider, FakeLoggerFactory? loggerFactory = null)
        {
            var services = new ServiceCollection()
                .AddSingleton<IJSRuntime>(browser.Runtime)
                .AddSingleton<ICultureProvider>(cultureProvider)
                .AddScoped<ICultureState>(_ => new FakeCultureState(CultureInfo.GetCultureInfo("en-US")));

            if (loggerFactory is not null)
            {
                services.AddSingleton<ILoggerFactory>(loggerFactory).AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            }

            return services.AddFormaCultureCoordination().BuildServiceProvider();
        }

        private static string DescribeLast(IServiceScope scope)
        {
            var applied = ((FakeCultureState)GetCultureState(scope)).AppliedCultures[^1];

            return $"{applied.Culture.Name} {applied.Culture.DateTimeFormat.ShortTimePattern}/{applied.UICulture.Name} {applied.UICulture.DateTimeFormat.ShortTimePattern}";
        }

        [Fact]
        public async Task Should_hand_the_state_the_provider_s_cultures_for_the_pair_the_page_accepted()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser("de-DE", "he");
            await using var provider = CreateServices(browser, new FakeCultureProvider());
            await using var scope = provider.CreateAsyncScope();

            // Act
            await GetRegistration(scope).RegisterAsync();

            // Assert
            Assert.Equal($"de-DE {FakeCultureProvider.ShortTimePattern}/he {FakeCultureProvider.ShortTimePattern}", DescribeLast(scope));
        }

        [Fact]
        public async Task Should_hand_the_state_the_provider_s_cultures_for_each_pair_the_page_accepts()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            var cultureProvider = new FakeCultureProvider();
            await using var provider = CreateServices(browser, cultureProvider);
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();

            // Act
            await browser.LandAsync("es-419", "he");

            // Assert
            Assert.Multiple(
                () => Assert.Equal($"es-419 {FakeCultureProvider.ShortTimePattern}/he {FakeCultureProvider.ShortTimePattern}", DescribeLast(scope)),
                () => Assert.Equal(["en-US", "en-US", "es-419", "he"], cultureProvider.RequestedNames));
        }

        [Fact]
        public async Task Should_log_and_fail_a_delivery_whose_culture_the_provider_returns_under_another_name()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeCultureCoordinatorBrowser();
            var cultureProvider = new FakeCultureProvider();
            await using var provider = CreateServices(browser, cultureProvider, loggerFactory);
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();
            cultureProvider.ReturnedName = "fr-FR";

            // Act
            var exception = await Record.ExceptionAsync(() => browser.LandAsync("de-DE", "he"));

            // Assert — a culture the application offers that its provider fails on is the provider's fault, logged without the names the browser sent
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.IsType<InvalidOperationException>(exception),
                () => Assert.Equal(LogLevel.Error, entry.Level),
                () => Assert.DoesNotContain("de-DE", entry.Message, StringComparison.Ordinal),
                () => Assert.Single(((FakeCultureState)GetCultureState(scope)).AppliedCultures));
        }
    }

    public sealed class Refusing
    {
        private static ServiceProvider CreateServices(FakeCultureCoordinatorBrowser browser, FakeLoggerFactory loggerFactory)
            => new ServiceCollection()
                .AddSingleton<IJSRuntime>(browser.Runtime)
                .AddSingleton<ILoggerFactory>(loggerFactory)
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .AddFormaCultureCoordination()
                .BuildServiceProvider();

        private static CultureCoordinatorReceiver GetReceiver(FakeCultureCoordinatorBrowser browser)
            => ((DotNetObjectReference<CultureCoordinatorReceiver>)Assert.Single(browser.Module.Recorder.GetCalls("register")).Arguments[1]!).Value;

        [Theory]
        [InlineData(null, "he", "document", 1)]
        [InlineData("de-DE", null, "document", 1)]
        [InlineData("de\0DE", "he", "document", 1)]
        [InlineData("de-DE", "h" + LoneSurrogate.Placeholder + "e", "document", 1)]
        [InlineData("de-DE; Path=/", "he", "document", 1)]
        [InlineData("zz-NOWHERE-of-no-kind", "he", "document", 1)]
        [InlineData("qq-ZZ", "he", "document", 1)]
        [InlineData("de-DE", "xyz", "document", 1)]
        [InlineData("de-DE_phoneb", "he", "document", 1)]
        [InlineData("x-hostile", "he", "document", 1)]
        [InlineData("de-DE", "he", null, 1)]
        [InlineData("de-DE", "he", "document", -1)]
        public async Task Should_refuse_and_log_a_pair_the_coordinator_couldn_t_have_delivered(string? culture, string? uiCulture, string? documentKey, int number)
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser, loggerFactory);
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();

            // Act
            var exception = await Record.ExceptionAsync(() => GetReceiver(browser).ApplyAsync(culture!, uiCulture is null ? null! : LoneSurrogate.Restore(uiCulture), documentKey!, number));

            // Assert
            var state = GetCultureState(scope);
            Assert.Multiple(
                () => Assert.Null(exception),
                () => Assert.Equal("en-US/en-US", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Contains(loggerFactory.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("and was refused", StringComparison.Ordinal)));
        }

        [Theory]
        [InlineData("qq-ZZ", "he")]
        [InlineData("de-DE", "xyz")]
        [InlineData("x-hostile", "he")]
        public async Task Should_refuse_and_log_a_culture_outside_the_application_s_cultures_before_the_provider_is_asked(string culture, string uiCulture)
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeCultureCoordinatorBrowser();
            var cultureProvider = new FakeCultureProvider();
            await using var provider = new ServiceCollection()
                .AddSingleton<IJSRuntime>(browser.Runtime)
                .AddSingleton<ILoggerFactory>(loggerFactory)
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .AddSingleton<ICultureProvider>(cultureProvider)
                .AddFormaCultureCoordination()
                .BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();
            var requestedCount = cultureProvider.RequestedNames.Count;

            // Act
            var exception = await Record.ExceptionAsync(() => GetReceiver(browser).ApplyAsync(culture, uiCulture, browser.DocumentKey, 1));

            // Assert
            Assert.Multiple(
                () => Assert.Null(exception),
                () => Assert.Equal(requestedCount, cultureProvider.RequestedNames.Count),
                () => Assert.Equal("en-US", GetCultureState(scope).Culture.Name),
                () => Assert.DoesNotContain(loggerFactory.Entries, entry => entry.Level > LogLevel.Debug),
                () => Assert.Contains(loggerFactory.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("outside the application's cultures", StringComparison.Ordinal)));
        }

        [Fact]
        public async Task Should_refuse_and_log_a_pair_naming_a_culture_longer_than_any_locale_name()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser, loggerFactory);
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();

            // Act
            var exception = await Record.ExceptionAsync(() => GetReceiver(browser).ApplyAsync(new string('a', 1024 * 1024), "he", browser.DocumentKey, 1));

            // Assert
            Assert.Multiple(
                () => Assert.Null(exception),
                () => Assert.Equal("en-US", GetCultureState(scope).Culture.Name),
                () => Assert.DoesNotContain(loggerFactory.Entries, entry => entry.Message.Contains("aaaa", StringComparison.Ordinal)));
        }

        [Theory]
        [InlineData(null, "he")]
        [InlineData("news/the-final", null)]
        public async Task Should_refuse_and_log_a_destination_naming_no_address_or_no_route(string? relativeAddress, string? token)
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeCultureCoordinatorBrowser();
            var services = new ServiceCollection()
                .AddSingleton<IJSRuntime>(browser.Runtime)
                .AddSingleton<ILoggerFactory>(loggerFactory)
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .AddSingleton<ICultureDestinationSource>(new ThrowingDestinationSource());
            await using var provider = services.AddFormaCultureCoordination().BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();

            // Act
            var destination = await GetReceiver(browser).ResolveDestinationAsync(relativeAddress!, token!);

            // Assert
            Assert.Multiple(
                () => Assert.Null(destination),
                () => Assert.DoesNotContain(loggerFactory.Entries, entry => entry.Level > LogLevel.Debug),
                () => Assert.Contains(loggerFactory.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("and was refused", StringComparison.Ordinal)));
        }

        // A source that throws on whatever it is asked, as one handed nothing to resolve would.
        private sealed class ThrowingDestinationSource : ICultureDestinationSource
        {
            public bool ResolvesDestinations
                => true;

            public ValueTask<string?> ResolveAsync(string relativeAddress, string token, CancellationToken cancellationToken)
                => throw new InvalidOperationException("The source was asked for nothing.");
        }
    }

    public sealed class Destinations
    {
        private static ServiceProvider CreateServices(FakeCultureCoordinatorBrowser browser, ICultureDestinationSource? source, FakeLoggerFactory? loggerFactory = null)
        {
            var services = new ServiceCollection().AddSingleton<IJSRuntime>(browser.Runtime);

            if (source is not null)
            {
                services.AddSingleton(source);
            }

            if (loggerFactory is not null)
            {
                services.AddSingleton<ILoggerFactory>(loggerFactory).AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            }

            return services.AddFormaCultureCoordination().BuildServiceProvider();
        }

        private static CultureCoordinatorReceiver GetReceiver(FakeCultureCoordinatorBrowser browser)
            => ((DotNetObjectReference<CultureCoordinatorReceiver>)Assert.Single(browser.Module.Recorder.GetCalls("register")).Arguments[1]!).Value;

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task Should_tell_the_page_whether_its_source_names_destinations(bool resolves, bool expected)
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser, new FakeDestinationSource { ResolvesDestinations = resolves });
            await using var scope = provider.CreateAsyncScope();

            // Act
            await GetRegistration(scope).RegisterAsync();

            // Assert
            Assert.Equal(expected, Assert.Single(browser.Module.Recorder.GetCalls("register")).Arguments[4]);
        }

        [Fact]
        public async Task Should_tell_the_page_it_names_no_destination_without_a_source()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser, source: null);
            await using var scope = provider.CreateAsyncScope();

            // Act
            await GetRegistration(scope).RegisterAsync();

            // Assert
            Assert.Equal(false, Assert.Single(browser.Module.Recorder.GetCalls("register")).Arguments[4]);
        }

        [Fact]
        public async Task Should_hand_the_page_the_destination_its_source_names()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            var source = new FakeDestinationSource { Answer = (address, token) => $"/{token}/hadashot/hagmar?from={address}" };
            await using var provider = CreateServices(browser, source);
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();

            // Act
            var destination = await GetReceiver(browser).ResolveDestinationAsync("news/the-final", "he");

            // Assert
            Assert.Equal("/he/hadashot/hagmar?from=news/the-final", destination);
        }

        [Fact]
        public async Task Should_log_a_source_that_throws_and_leave_the_pages_own_address()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeCultureCoordinatorBrowser();
            var source = new FakeDestinationSource { Answer = (_, _) => throw new InvalidOperationException("The story's addresses in other cultures couldn't be read.") };
            await using var provider = CreateServices(browser, source, loggerFactory);
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();

            // Act
            var destination = await GetReceiver(browser).ResolveDestinationAsync("news/the-final", "hostile-token");

            // Assert — the source's failure is the host's, logged without the token or the address the browser sent
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Null(destination),
                () => Assert.Equal(LogLevel.Error, entry.Level),
                () => Assert.DoesNotContain("hostile-token", entry.Message, StringComparison.Ordinal),
                () => Assert.DoesNotContain("news/the-final", entry.Message, StringComparison.Ordinal));
        }

        // A source that names destinations through `Answer`.
        private sealed class FakeDestinationSource : ICultureDestinationSource
        {
            public bool ResolvesDestinations { get; init; } = true;

            public Func<string, string, string?> Answer { get; init; } = (_, _) => null;

            public ValueTask<string?> ResolveAsync(string relativeAddress, string token, CancellationToken cancellationToken)
                => ValueTask.FromResult(Answer(relativeAddress, token));
        }
    }
}
