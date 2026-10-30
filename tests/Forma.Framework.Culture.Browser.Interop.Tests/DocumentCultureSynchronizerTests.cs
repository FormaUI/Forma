namespace Forma.Framework.Culture.Browser.Interop.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

public sealed class DocumentCultureSynchronizerTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private static ServiceProvider CreateServices(FakeDocumentCultureSynchronizerBrowser browser)
        => new ServiceCollection()
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddScoped<ICultureSwitch, TestCultureSwitch>()
            .AddFormaCultureBrowserSynchronization()
            .BuildServiceProvider();

    // A scope whose culture route handler changes its state to the culture an address's first segment names, English where it names none.
    private static ServiceProvider CreateServicesFollowingAddresses(FakeDocumentCultureSynchronizerBrowser browser)
        => new ServiceCollection()
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddScoped(provider => new FakeCultureRouteHandler(address => provider.GetRequiredService<ICultureSwitch>().SwitchAsync(
                CultureInfo.GetCultureInfo(address.AbsolutePath.StartsWith("/he/", StringComparison.Ordinal) ? "he-IL" : "en-US"))))
            .AddScoped<ICultureRouteHandler>(provider => provider.GetRequiredService<FakeCultureRouteHandler>())
            .AddScoped<ICultureSwitch, TestCultureSwitch>()
            .AddFormaCultureBrowserSynchronization()
            .BuildServiceProvider();

    private static DocumentCultureSynchronizer GetDocumentCultureSynchronizer(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<DocumentCultureSynchronizer>();

    private static ICultureState GetCultureState(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<ICultureState>();

    private static ICultureSwitch GetCultureSwitch(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<ICultureSwitch>();

    public sealed class Following
    {
        [Fact]
        public async Task Should_set_the_document_to_the_states_pair_as_it_starts()
        {
            // Arrange — a scope whose state starts in Hebrew, in a document the server served in English
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser("en", "ltr");
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            GetDocumentCultureSynchronizer(scope).BeginFollowing();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(("he-IL", "rtl"), browser.Document),
                () => Assert.NotNull(browser.OwnerIdentity));
        }

        [Fact]
        public async Task Should_set_the_document_to_each_change_of_the_state()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            GetDocumentCultureSynchronizer(scope).BeginFollowing();

            // Act
            await GetCultureSwitch(scope).SwitchAsync(German, French);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(("fr-FR", "ltr"), browser.Document),
                () => Assert.Equal([("fr-FR", "ltr")], browser.Updates));
        }

        [Fact]
        public async Task Should_apply_a_change_made_during_an_update_and_send_nothing_for_the_revisions_it_covers()
        {
            // Arrange — the first update held open while two more changes arrive
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var cultureSwitch = GetCultureSwitch(scope);
            GetDocumentCultureSynchronizer(scope).BeginFollowing();
            browser.OwnerRegistration!.Recorder.Hold("update");
            var first = cultureSwitch.SwitchAsync(CultureInfo.GetCultureInfo("he-IL"));
            var second = cultureSwitch.SwitchAsync(German);
            var third = cultureSwitch.SwitchAsync(French);

            // Act
            browser.OwnerRegistration.Recorder.Release("update");
            Assert.Single(browser.OwnerRegistration.Recorder.GetCalls("update")).Complete(true);
            await Task.WhenAll(first, second, third);

            // Assert — the held update, then one for the latest state; the last reconciliation finds it applied
            Assert.Multiple(
                () => Assert.Equal(2, browser.OwnerRegistration!.Recorder.GetCalls("update").Count),
                () => Assert.Equal(("fr-FR", "ltr"), browser.Document));
        }
    }

    public sealed class Recovering
    {
        [Fact]
        public async Task Should_answer_a_connection_that_came_up_while_a_registration_was_in_flight()
        {
            // Arrange — the first registration held open while the connection comes up again
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            browser.Module.Recorder.Hold("register");
            await using var firstRegistration = new RecordingJSObjectReference((_, _) => true);
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();
            documentCultureSynchronizer.BeginFollowing();
            browser.Module.Recorder.Release("register");

            // Act — a change queued behind both reconciliations completes once they have
            Assert.Single(browser.Module.Recorder.GetCalls("register")).Complete(firstRegistration);
            await GetCultureSwitch(scope).SwitchAsync(German);

            // Assert — the later connection registered again rather than taking the first registration as its own
            Assert.Multiple(
                () => Assert.Equal(2, browser.CountRegistrations()),
                () => Assert.Equal(("de-DE", "ltr"), browser.Document));
        }

        [Fact]
        public async Task Should_recover_a_registration_the_browser_made_but_whose_result_was_lost()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            browser.LoseNextResult("register", new JSDisconnectedException("The circuit dropped."));
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();
            var owner = browser.OwnerIdentity;

            // Act — the connection comes up again
            documentCultureSynchronizer.BeginFollowing();
            await GetCultureSwitch(scope).SwitchAsync(German);

            // Assert — the same identity recovered the registration rather than being refused as another owner
            Assert.Multiple(
                () => Assert.Equal(owner, browser.OwnerIdentity),
                () => Assert.Equal(2, browser.CountRegistrations()),
                () => Assert.Equal(("de-DE", "ltr"), browser.Document));
        }

        [Fact]
        public async Task Should_import_again_after_an_import_the_connection_dropped()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            browser.FailNext("import", new JSDisconnectedException("The circuit dropped."));
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();

            // Act
            documentCultureSynchronizer.BeginFollowing();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(2, browser.CountImports()),
                () => Assert.Equal(("he-IL", "rtl"), browser.Document));
        }

        [Fact]
        public async Task Should_import_again_after_a_cancelled_import()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            browser.FailNext("import", new TaskCanceledException());
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();

            // Act
            documentCultureSynchronizer.BeginFollowing();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(2, browser.CountImports()),
                () => Assert.Equal(("he-IL", "rtl"), browser.Document));
        }

        [Fact]
        public async Task Should_send_a_change_the_connection_dropped_once_it_comes_up_again()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();
            browser.FailNext("update", new JSDisconnectedException("The circuit dropped."));
            await GetCultureSwitch(scope).SwitchAsync(German);

            // Act
            documentCultureSynchronizer.BeginFollowing();

            // Assert
            Assert.Equal(("de-DE", "ltr"), browser.Document);
        }

        [Fact]
        public async Task Should_survive_a_cancelled_update_and_apply_the_next_change()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var cultureSwitch = GetCultureSwitch(scope);
            GetDocumentCultureSynchronizer(scope).BeginFollowing();
            browser.FailNext("update", new TaskCanceledException());
            await cultureSwitch.SwitchAsync(German);

            // Act
            await cultureSwitch.SwitchAsync(French);

            // Assert
            Assert.Equal(("fr-FR", "ltr"), browser.Document);
        }

        [Fact]
        public async Task Should_register_again_as_the_connection_comes_up_after_the_browser_released_it()
        {
            // Arrange — the state has not changed since the registration the browser released
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();
            browser.CloseCircuit();

            // Act
            documentCultureSynchronizer.BeginFollowing();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(("he-IL", "rtl"), browser.Document),
                () => Assert.NotNull(browser.OwnerIdentity));
        }

        [Fact]
        public async Task Should_register_again_when_the_browser_reports_the_registration_no_longer_owns_the_document()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            GetDocumentCultureSynchronizer(scope).BeginFollowing();
            browser.CloseCircuit();

            // Act
            await GetCultureSwitch(scope).SwitchAsync(German);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(("de-DE", "ltr"), browser.Document),
                () => Assert.Equal(2, browser.CountRegistrations()));
        }
    }

    public sealed class Landing
    {
        [Fact]
        public async Task Should_register_without_a_receiver_where_no_culture_route_handler_is_registered()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            GetDocumentCultureSynchronizer(scope).BeginFollowing();

            // Assert
            Assert.Multiple(
                () => Assert.NotNull(browser.OwnerIdentity),
                () => Assert.Null(browser.OwnerReceiver));
        }

        [Fact]
        public async Task Should_register_a_receiver_where_a_culture_route_handler_is_registered()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServicesFollowingAddresses(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            GetDocumentCultureSynchronizer(scope).BeginFollowing();

            // Assert
            Assert.NotNull(browser.OwnerReceiver);
        }

        [Fact]
        public async Task Should_reply_once_the_browser_holds_the_pair_the_landed_address_leaves_the_state_in()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServicesFollowingAddresses(browser);
            await using var scope = provider.CreateAsyncScope();
            GetDocumentCultureSynchronizer(scope).BeginFollowing();

            // Act
            var holdsPair = await browser.ReportLandingAsync("http://localhost/he/about");

            // Assert
            Assert.Multiple(
                () => Assert.True(holdsPair),
                () => Assert.Equal([new Uri("http://localhost/he/about")], scope.ServiceProvider.GetRequiredService<FakeCultureRouteHandler>().Addresses),
                () => Assert.Equal([("he-IL", "rtl")], browser.Updates));
        }

        [Fact]
        public async Task Should_reply_for_a_landing_within_one_culture_though_the_state_announces_no_change()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServicesFollowingAddresses(browser);
            await using var scope = provider.CreateAsyncScope();
            GetDocumentCultureSynchronizer(scope).BeginFollowing();

            // Act
            var holdsPair = await browser.ReportLandingAsync("http://localhost/about");

            // Assert — the browser already holds the pair the registration sent, so nothing more is sent
            Assert.Multiple(
                () => Assert.True(holdsPair),
                () => Assert.Equal(0, GetCultureState(scope).Revision),
                () => Assert.Empty(browser.Updates));
        }

        [Fact]
        public async Task Should_reply_only_once_the_browser_has_taken_the_pair()
        {
            // Arrange — the update the landing's change sends held open
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServicesFollowingAddresses(browser);
            await using var scope = provider.CreateAsyncScope();
            GetDocumentCultureSynchronizer(scope).BeginFollowing();
            var registration = browser.OwnerRegistration!;
            registration.Recorder.Hold("update");
            var reply = browser.ReportLandingAsync("http://localhost/he/about");
            var isAnsweredWhileHeld = reply.IsCompleted;

            // Act
            registration.Recorder.Release("update");
            Assert.Single(registration.Recorder.GetCalls("update")).Complete(true);
            var holdsPair = await reply;

            // Assert
            Assert.Multiple(
                () => Assert.False(isAnsweredWhileHeld),
                () => Assert.True(holdsPair));
        }

        [Fact]
        public async Task Should_send_a_change_made_while_the_report_is_pending_and_reply_once_the_browser_holds_it()
        {
            // Arrange — the landing's update held open while the state changes in place
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServicesFollowingAddresses(browser);
            await using var scope = provider.CreateAsyncScope();
            GetDocumentCultureSynchronizer(scope).BeginFollowing();
            var registration = browser.OwnerRegistration!;
            registration.Recorder.Hold("update");
            var reply = browser.ReportLandingAsync("http://localhost/he/about");
            var change = GetCultureSwitch(scope).SwitchAsync(German);

            // Act
            registration.Recorder.Release("update");
            Assert.Single(registration.Recorder.GetCalls("update")).Complete(true);
            await change;
            var holdsPair = await reply;

            // Assert — the reply came once the browser held a pair at least as new as the landing's
            Assert.Multiple(
                () => Assert.True(holdsPair),
                () => Assert.Equal(("de-DE", "ltr"), browser.Document));
        }

        [Fact]
        public async Task Should_refuse_the_reply_while_the_browser_lacks_the_pair()
        {
            // Arrange — a dropped connection, which fails the change's update and the landing's own reconciliation alike
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServicesFollowingAddresses(browser);
            await using var scope = provider.CreateAsyncScope();
            GetDocumentCultureSynchronizer(scope).BeginFollowing();
            browser.FailNext("update", new JSDisconnectedException("The circuit dropped."));
            browser.FailNext("update", new JSDisconnectedException("The circuit dropped."));

            // Act
            var holdsPair = await browser.ReportLandingAsync("http://localhost/he/about");

            // Assert
            Assert.Multiple(
                () => Assert.False(holdsPair),
                () => Assert.Equal(("en-US", "ltr"), browser.Document));
        }

        [Fact]
        public async Task Should_log_a_culture_route_handler_that_throws_and_reply_once_the_browser_holds_the_pair()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = new ServiceCollection()
                .AddSingleton<IJSRuntime>(browser.Runtime)
                .AddSingleton<ILogger<DocumentCultureSynchronizer>>(new Logger<DocumentCultureSynchronizer>(loggerFactory))
                .AddScoped<ICultureRouteHandler>(_ => new FakeCultureRouteHandler(_ => throw new InvalidOperationException("The handler failed.")))
                .AddScoped<ICultureSwitch, TestCultureSwitch>()
                .AddFormaCultureBrowserSynchronization()
                .BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            GetDocumentCultureSynchronizer(scope).BeginFollowing();

            // Act
            var holdsPair = await browser.ReportLandingAsync("http://localhost/he/about");

            // Assert
            Assert.Multiple(
                () => Assert.True(holdsPair),
                () => Assert.Equal(("en-US", "ltr"), browser.Document),
                () => Assert.Equal(LogLevel.Error, Assert.Single(loggerFactory.Entries).Level));
        }

        [Fact]
        public async Task Should_reply_to_a_landing_reported_again_once_the_connection_comes_up()
        {
            // Arrange — a report refused while the connection was down
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServicesFollowingAddresses(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();
            browser.FailNext("update", new JSDisconnectedException("The circuit dropped."));
            browser.FailNext("update", new JSDisconnectedException("The circuit dropped."));
            await browser.ReportLandingAsync("http://localhost/he/about");
            documentCultureSynchronizer.BeginFollowing();

            // Act
            var holdsPair = await browser.ReportLandingAsync("http://localhost/he/about");

            // Assert
            Assert.Multiple(
                () => Assert.True(holdsPair),
                () => Assert.Equal(("he-IL", "rtl"), browser.Document));
        }
    }

    public sealed class Disposing
    {
        [Fact]
        public async Task Should_send_nothing_for_the_changes_queued_behind_an_update_as_it_is_disposed()
        {
            // Arrange — an update held open with two more changes queued behind it
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var cultureSwitch = GetCultureSwitch(scope);
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();
            var registration = browser.OwnerRegistration!;
            registration.Recorder.Hold("update");
            var changes = Task.WhenAll(cultureSwitch.SwitchAsync(German), cultureSwitch.SwitchAsync(French));
            var disposal = documentCultureSynchronizer.DisposeAsync().AsTask();

            // Act
            registration.Recorder.Release("update");
            Assert.Single(registration.Recorder.GetCalls("update")).Complete(true);
            await Task.WhenAll(changes, disposal);

            // Assert — the queued changes sent nothing once disposed, and the document returned to what was served
            Assert.Multiple(
                () => Assert.Single(registration.Recorder.GetCalls("update")),
                () => Assert.Equal(("en", "ltr"), browser.Document),
                () => Assert.Null(browser.OwnerIdentity));
        }

        [Fact]
        public async Task Should_release_the_document_to_the_served_pair()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser("en", "ltr");
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();

            // Act
            await documentCultureSynchronizer.DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(("en", "ltr"), browser.Document),
                () => Assert.Null(browser.OwnerIdentity));
        }

        [Fact]
        public async Task Should_stop_following_the_state()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentCultureSynchronizerBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentCultureSynchronizer = GetDocumentCultureSynchronizer(scope);
            documentCultureSynchronizer.BeginFollowing();
            await documentCultureSynchronizer.DisposeAsync();

            // Act
            await GetCultureSwitch(scope).SwitchAsync(German);
            documentCultureSynchronizer.BeginFollowing();

            // Assert
            Assert.Multiple(
                () => Assert.Empty(browser.Updates),
                () => Assert.Equal(1, browser.CountRegistrations()));
        }
    }
}
