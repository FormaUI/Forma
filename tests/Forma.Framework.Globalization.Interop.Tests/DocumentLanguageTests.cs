namespace Forma.Framework.Globalization.Interop.Tests;

using System.Globalization;

using Forma.Tests.Globalization;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

public sealed class DocumentLanguageTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private static ServiceProvider CreateServices(FakeDocumentLanguageBrowser browser)
        => new ServiceCollection()
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddFormaDocumentLanguage()
            .BuildServiceProvider();

    private static DocumentLanguage GetDocumentLanguage(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<DocumentLanguage>();

    private static ICultureState GetCultureState(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<ICultureState>();

    public sealed class Following
    {
        [Fact]
        public async Task Should_set_the_document_to_the_states_pair_as_it_starts()
        {
            // Arrange — a scope whose state starts in Hebrew, in a document the server served in English
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentLanguageBrowser("en", "ltr");
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            GetDocumentLanguage(scope).BeginFollowing();

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
            var browser = new FakeDocumentLanguageBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            GetDocumentLanguage(scope).BeginFollowing();

            // Act
            await GetCultureState(scope).ChangeAsync(German, French);

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
            var browser = new FakeDocumentLanguageBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var cultureState = GetCultureState(scope);
            GetDocumentLanguage(scope).BeginFollowing();
            browser.OwnerRegistration!.Recorder.Hold("update");
            var first = cultureState.ChangeAsync(CultureInfo.GetCultureInfo("he-IL"));
            var second = cultureState.ChangeAsync(German);
            var third = cultureState.ChangeAsync(French);

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
            var browser = new FakeDocumentLanguageBrowser();
            browser.Module.Recorder.Hold("register");
            await using var firstRegistration = new RecordingJSObjectReference((_, _) => true);
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentLanguage = GetDocumentLanguage(scope);
            documentLanguage.BeginFollowing();
            documentLanguage.BeginFollowing();
            browser.Module.Recorder.Release("register");

            // Act — a change queued behind both reconciliations completes once they have
            Assert.Single(browser.Module.Recorder.GetCalls("register")).Complete(firstRegistration);
            await GetCultureState(scope).ChangeAsync(German);

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
            var browser = new FakeDocumentLanguageBrowser();
            browser.LoseNextResult("register", new JSDisconnectedException("The circuit dropped."));
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentLanguage = GetDocumentLanguage(scope);
            documentLanguage.BeginFollowing();
            var owner = browser.OwnerIdentity;

            // Act — the connection comes up again
            documentLanguage.BeginFollowing();
            await GetCultureState(scope).ChangeAsync(German);

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
            var browser = new FakeDocumentLanguageBrowser();
            browser.FailNext("import", new JSDisconnectedException("The circuit dropped."));
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentLanguage = GetDocumentLanguage(scope);
            documentLanguage.BeginFollowing();

            // Act
            documentLanguage.BeginFollowing();

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
            var browser = new FakeDocumentLanguageBrowser();
            browser.FailNext("import", new TaskCanceledException());
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentLanguage = GetDocumentLanguage(scope);
            documentLanguage.BeginFollowing();

            // Act
            documentLanguage.BeginFollowing();

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
            var browser = new FakeDocumentLanguageBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentLanguage = GetDocumentLanguage(scope);
            documentLanguage.BeginFollowing();
            browser.FailNext("update", new JSDisconnectedException("The circuit dropped."));
            await GetCultureState(scope).ChangeAsync(German);

            // Act
            documentLanguage.BeginFollowing();

            // Assert
            Assert.Equal(("de-DE", "ltr"), browser.Document);
        }

        [Fact]
        public async Task Should_survive_a_cancelled_update_and_apply_the_next_change()
        {
            // Arrange
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentLanguageBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var cultureState = GetCultureState(scope);
            GetDocumentLanguage(scope).BeginFollowing();
            browser.FailNext("update", new TaskCanceledException());
            await cultureState.ChangeAsync(German);

            // Act
            await cultureState.ChangeAsync(French);

            // Assert
            Assert.Equal(("fr-FR", "ltr"), browser.Document);
        }

        [Fact]
        public async Task Should_register_again_as_the_connection_comes_up_after_the_browser_released_it()
        {
            // Arrange — the state has not changed since the registration the browser released
            using var fixture = new CultureFixture("he-IL");
            var browser = new FakeDocumentLanguageBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentLanguage = GetDocumentLanguage(scope);
            documentLanguage.BeginFollowing();
            browser.CloseCircuit();

            // Act
            documentLanguage.BeginFollowing();

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
            var browser = new FakeDocumentLanguageBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            GetDocumentLanguage(scope).BeginFollowing();
            browser.CloseCircuit();

            // Act
            await GetCultureState(scope).ChangeAsync(German);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(("de-DE", "ltr"), browser.Document),
                () => Assert.Equal(2, browser.CountRegistrations()));
        }
    }

    public sealed class Disposing
    {
        [Fact]
        public async Task Should_send_nothing_for_the_changes_queued_behind_an_update_as_it_is_disposed()
        {
            // Arrange — an update held open with two more changes queued behind it
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeDocumentLanguageBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var cultureState = GetCultureState(scope);
            var documentLanguage = GetDocumentLanguage(scope);
            documentLanguage.BeginFollowing();
            var registration = browser.OwnerRegistration!;
            registration.Recorder.Hold("update");
            var changes = Task.WhenAll(cultureState.ChangeAsync(German), cultureState.ChangeAsync(French));
            var disposal = documentLanguage.DisposeAsync().AsTask();

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
            var browser = new FakeDocumentLanguageBrowser("en", "ltr");
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentLanguage = GetDocumentLanguage(scope);
            documentLanguage.BeginFollowing();

            // Act
            await documentLanguage.DisposeAsync();

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
            var browser = new FakeDocumentLanguageBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var documentLanguage = GetDocumentLanguage(scope);
            documentLanguage.BeginFollowing();
            await documentLanguage.DisposeAsync();

            // Act
            await GetCultureState(scope).ChangeAsync(German);
            documentLanguage.BeginFollowing();

            // Assert
            Assert.Multiple(
                () => Assert.Empty(browser.Updates),
                () => Assert.Equal(1, browser.CountRegistrations()));
        }
    }
}
