namespace Forma.Framework.Culture.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class DefaultCultureStateTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    private static ServiceProvider CreateServices()
        => new ServiceCollection().AddFormaCultureServices().BuildServiceProvider();

    private static int _lastNumber;

    private static ICultureState GetState(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<ICultureState>();

    private static ICultureStateIntegration GetIntegration(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<ICultureStateIntegration>();

    // Each pair under the next version of one document, so every application follows the one before it.
    private static Task ApplyAsync(IServiceScope scope, CultureInfo culture, CultureInfo? uiCulture = null)
        => GetIntegration(scope).ApplyAsync(new AcceptedCulture(culture, uiCulture ?? culture, new CultureVersion("test", Interlocked.Increment(ref _lastNumber))));

    public sealed class Creation
    {
        [Fact]
        public void Should_start_from_the_cultures_in_force_when_the_scope_resolves_it()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            CultureInfo.CurrentCulture = German;
            using var services = CreateServices();
            using var scope = services.CreateScope();

            // Act
            var state = GetState(scope);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(German, state.Culture),
                () => Assert.Equal(English, state.UICulture),
                () => Assert.False(state.IsRightToLeft),
                () => Assert.Equal(0, state.Revision),
                () => Assert.Same(CultureVersion.Initial, state.Version));
        }

        [Fact]
        public void Should_hold_one_state_per_scope()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            using var otherScope = services.CreateScope();

            // Act
            var state = GetState(scope);

            // Assert
            Assert.Multiple(
                () => Assert.Same(state, GetState(scope)),
                () => Assert.NotSame(state, GetState(otherScope)));
        }
    }

    public sealed class Application
    {
        [Fact]
        public async Task Should_take_the_culture_as_the_UI_culture_when_none_is_given()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);

            // Act
            await ApplyAsync(scope, Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(Hebrew, state.Culture),
                () => Assert.Equal(Hebrew, state.UICulture));
        }

        [Fact]
        public async Task Should_hold_the_culture_and_the_UI_culture_apart()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);

            // Act
            await ApplyAsync(scope, German, Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(German, state.Culture),
                () => Assert.Equal(Hebrew, state.UICulture));
        }

        [Theory]
        [InlineData("en", "he", true)]
        [InlineData("he", "en", false)]
        public async Task Should_read_the_direction_from_the_UI_culture(string culture, string uiCulture, bool isRightToLeft)
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);

            // Act
            await ApplyAsync(scope, CultureInfo.GetCultureInfo(culture), CultureInfo.GetCultureInfo(uiCulture));

            // Assert
            Assert.Equal(isRightToLeft, state.IsRightToLeft);
        }

        [Fact]
        public async Task Should_hold_read_only_cultures()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var writableCulture = new CultureInfo("he");

            // Act
            await ApplyAsync(scope, writableCulture);

            // Assert
            Assert.Multiple(
                () => Assert.False(writableCulture.IsReadOnly),
                () => Assert.True(state.Culture.IsReadOnly),
                () => Assert.True(state.UICulture.IsReadOnly));
        }

        [Fact]
        public async Task Should_advance_the_revision_once_per_change()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);

            // Act
            await ApplyAsync(scope, Hebrew);
            await ApplyAsync(scope, German, Hebrew);

            // Assert
            Assert.Equal(2, state.Revision);
        }

        [Fact]
        public async Task Should_make_no_change_to_the_cultures_already_held()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var notifications = 0;
            state.Subscribe(() => { notifications++; return Task.CompletedTask; });

            // Act
            await ApplyAsync(scope, new CultureInfo("en"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(0, state.Revision),
                () => Assert.Equal(0, notifications));
        }

        [Fact]
        public async Task Should_change_to_a_culture_of_the_same_name_that_writes_times_otherwise()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var notifications = 0;
            state.Subscribe(() => { notifications++; return Task.CompletedTask; });
            var clock = (CultureInfo)CultureInfo.GetCultureInfo("en").Clone();
            clock.DateTimeFormat.ShortTimePattern = "HH:mm";

            // Act
            await ApplyAsync(scope, clock);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, state.Revision),
                () => Assert.Equal(1, notifications),
                () => Assert.Equal("HH:mm", state.Culture.DateTimeFormat.ShortTimePattern));
        }

        [Fact]
        public async Task Should_change_to_a_culture_of_the_same_name_that_reads_two_digit_years_otherwise()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var century = (CultureInfo)CultureInfo.GetCultureInfo("en").Clone();
            century.DateTimeFormat.Calendar.TwoDigitYearMax = 2099;

            // Act
            await ApplyAsync(scope, century);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, state.Revision),
                () => Assert.Equal(2099, state.Culture.DateTimeFormat.Calendar.TwoDigitYearMax));
        }

        [Fact]
        public async Task Should_apply_the_cultures_to_the_calling_flow()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            state.Subscribe(async () => await Task.Yield());

            // Act
            await ApplyAsync(scope, German, Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(German, CultureInfo.CurrentCulture),
                () => Assert.Equal(Hebrew, CultureInfo.CurrentUICulture));
        }

        [Fact]
        public async Task Should_apply_the_cultures_already_held_to_a_calling_flow_holding_others_without_a_change()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var notifications = 0;
            state.Subscribe(() => { notifications++; return Task.CompletedTask; });
            CultureInfo.CurrentCulture = German;
            CultureInfo.CurrentUICulture = Hebrew;

            // Act
            await ApplyAsync(scope, new CultureInfo("en"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(English, CultureInfo.CurrentCulture),
                () => Assert.Equal(English, CultureInfo.CurrentUICulture),
                () => Assert.Equal(0, state.Revision),
                () => Assert.Equal(0, notifications));
        }

        [Fact]
        public void Should_hold_the_cultures_in_the_state_and_the_calling_flow_before_the_notification_completes()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var gate = new TaskCompletionSource();
            state.Subscribe(() => gate.Task);

            // Act
            var change = ApplyAsync(scope, German, Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.False(change.IsCompleted),
                () => Assert.Equal(German, state.Culture),
                () => Assert.Equal(Hebrew, state.UICulture),
                () => Assert.Equal(1, state.Revision),
                () => Assert.Equal(German, CultureInfo.CurrentCulture),
                () => Assert.Equal(Hebrew, CultureInfo.CurrentUICulture));
            gate.SetResult();
        }

        [Fact]
        public async Task Should_reject_a_missing_pair()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);

            // Act
            var exception = await Record.ExceptionAsync(() => GetIntegration(scope).ApplyAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }
    }

    public sealed class Versions
    {
        [Fact]
        public async Task Should_record_the_version_it_applies()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var version = new CultureVersion("document", 3);

            // Act
            await GetIntegration(scope).ApplyAsync(new AcceptedCulture(Hebrew, Hebrew, version));

            // Assert
            Assert.Same(version, GetState(scope).Version);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(1)]
        public async Task Should_change_nothing_for_a_version_of_the_same_document_it_has_reached(int number)
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var notifications = 0;
            await GetIntegration(scope).ApplyAsync(new AcceptedCulture(Hebrew, Hebrew, new CultureVersion("document", 2)));
            state.Subscribe(() => { notifications++; return Task.CompletedTask; });

            // Act
            await GetIntegration(scope).ApplyAsync(new AcceptedCulture(German, German, new CultureVersion("document", number)));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(Hebrew, state.Culture),
                () => Assert.Equal(2, state.Version.Number),
                () => Assert.Equal(1, state.Revision),
                () => Assert.Equal(0, notifications));
        }

        [Fact]
        public async Task Should_apply_a_version_of_another_document_whatever_its_number()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            await GetIntegration(scope).ApplyAsync(new AcceptedCulture(Hebrew, Hebrew, new CultureVersion("document", 5)));
            var version = new CultureVersion("next document", 0);

            // Act
            await GetIntegration(scope).ApplyAsync(new AcceptedCulture(German, German, version));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(German, GetState(scope).Culture),
                () => Assert.Same(version, GetState(scope).Version));
        }

        [Fact]
        public async Task Should_record_a_later_version_of_the_pair_it_holds_without_a_change()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            await GetIntegration(scope).ApplyAsync(new AcceptedCulture(Hebrew, Hebrew, new CultureVersion("document", 1)));
            var notifications = 0;
            state.Subscribe(() => { notifications++; return Task.CompletedTask; });

            // Act
            await GetIntegration(scope).ApplyAsync(new AcceptedCulture(new CultureInfo("he"), new CultureInfo("he"), new CultureVersion("document", 2)));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(2, state.Version.Number),
                () => Assert.Equal(1, state.Revision),
                () => Assert.Equal(0, notifications));
        }
    }

    public sealed class Notification
    {
        [Fact]
        public async Task Should_notify_in_the_new_cultures_and_revision()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var notifiedCultures = new List<string>();
            state.Subscribe(() =>
            {
                notifiedCultures.Add($"{CultureInfo.CurrentCulture.Name}/{CultureInfo.CurrentUICulture.Name} r{state.Revision}");
                return Task.CompletedTask;
            });

            // Act
            await ApplyAsync(scope, German, Hebrew);

            // Assert
            Assert.Equal(["de-DE/he r1"], notifiedCultures);
        }

        [Fact]
        public async Task Should_notify_its_subscribers_once_the_call_that_applied_the_pair_has_returned()
        {
            // Arrange — the applying flow's context runs what is posted to it as the test asks
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var notifications = 0;
            state.Subscribe(() => { notifications++; return Task.CompletedTask; });
            var context = new QueuedSynchronizationContext();
            Task change;

            // Act
            using (context.MakeCurrent())
            {
                change = ApplyAsync(scope, Hebrew);
            }

            var notifiedOnReturn = notifications;
            context.RunPending();
            await change;

            // Assert
            Assert.Multiple(
                () => Assert.Equal(0, notifiedOnReturn),
                () => Assert.Equal(1, notifications));
        }

        [Fact]
        public async Task Should_complete_once_every_subscriber_has_been_notified()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var gate = new TaskCompletionSource();
            var notifiedSubscribers = new List<string>();
            state.Subscribe(async () => { await gate.Task; notifiedSubscribers.Add("first"); });
            state.Subscribe(() => { notifiedSubscribers.Add("second"); return Task.CompletedTask; });

            // Act
            var change = ApplyAsync(scope, Hebrew);
            var isCompletedBeforeRelease = change.IsCompleted;
            gate.SetResult();
            await change;

            // Assert
            Assert.Multiple(
                () => Assert.False(isCompletedBeforeRelease),
                () => Assert.Equal(Hebrew, state.Culture),
                () => Assert.Equal(["first", "second"], notifiedSubscribers));
        }

        [Fact]
        public async Task Should_stop_notifying_an_unsubscribed_callback()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);
            var notifications = 0;
            Task CountAsync() { notifications++; return Task.CompletedTask; }
            state.Subscribe(CountAsync);
            await ApplyAsync(scope, Hebrew);

            // Act
            state.Unsubscribe(CountAsync);
            await ApplyAsync(scope, English);

            // Assert
            Assert.Equal(1, notifications);
        }

        [Fact]
        public async Task Should_leave_another_scope_unchanged()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            using var otherScope = services.CreateScope();
            var otherNotifications = 0;
            GetState(otherScope).Subscribe(() => { otherNotifications++; return Task.CompletedTask; });

            // Act
            await ApplyAsync(scope, Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(English, GetState(otherScope).Culture),
                () => Assert.Equal(0, GetState(otherScope).Revision),
                () => Assert.Equal(0, otherNotifications));
        }
    }

    public sealed class Provision
    {
        private static ServiceProvider CreateServices(FakeCultureProvider cultureProvider)
            => new ServiceCollection().AddSingleton<ICultureProvider>(cultureProvider).AddFormaCultureServices().BuildServiceProvider();

        [Fact]
        public void Should_start_in_the_provider_s_cultures_for_the_names_in_force()
        {
            // Arrange
            using var fixture = new CultureFixture("he");
            CultureInfo.CurrentCulture = German;
            using var services = CreateServices(new FakeCultureProvider());
            using var scope = services.CreateScope();

            // Act
            var state = GetState(scope);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Equal(FakeCultureProvider.ShortTimePattern, state.Culture.DateTimeFormat.ShortTimePattern),
                () => Assert.Equal(FakeCultureProvider.ShortTimePattern, state.UICulture.DateTimeFormat.ShortTimePattern));
        }

        [Fact]
        public async Task Should_apply_the_provider_s_cultures_for_the_names_a_pair_carries()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices(new FakeCultureProvider());
            using var scope = services.CreateScope();
            var state = GetState(scope);

            // Act
            await ApplyAsync(scope, German, Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Equal(FakeCultureProvider.ShortTimePattern, state.Culture.DateTimeFormat.ShortTimePattern),
                () => Assert.Equal(FakeCultureProvider.ShortTimePattern, state.UICulture.DateTimeFormat.ShortTimePattern),
                () => Assert.Equal(FakeCultureProvider.ShortTimePattern, CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern));
        }

        [Fact]
        public async Task Should_reject_a_culture_the_provider_returns_under_another_name_and_keep_its_pair()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var cultureProvider = new FakeCultureProvider();
            using var services = CreateServices(cultureProvider);
            using var scope = services.CreateScope();
            var state = GetState(scope);
            cultureProvider.ReturnedName = "fr-FR";

            // Act
            var exception = await Record.ExceptionAsync(() => ApplyAsync(scope, German));

            // Assert
            Assert.Multiple(
                () => Assert.IsType<InvalidOperationException>(exception),
                () => Assert.Equal("en/en", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Same(CultureVersion.Initial, state.Version));
        }
    }
}
