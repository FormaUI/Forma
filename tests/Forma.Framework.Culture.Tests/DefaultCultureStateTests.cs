namespace Forma.Framework.Culture.Tests;

using System.Globalization;

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

    private static ICultureState GetState(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<ICultureState>();

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
                () => Assert.Equal(0, state.Revision));
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

    public sealed class Change
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
            await state.ChangeAsync(Hebrew);

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
            await state.ChangeAsync(German, Hebrew);

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
            await state.ChangeAsync(CultureInfo.GetCultureInfo(culture), CultureInfo.GetCultureInfo(uiCulture));

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
            await state.ChangeAsync(writableCulture);

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
            await state.ChangeAsync(Hebrew);
            await state.ChangeAsync(German, Hebrew);

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
            await state.ChangeAsync(new CultureInfo("en"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(0, state.Revision),
                () => Assert.Equal(0, notifications));
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
            await state.ChangeAsync(German, Hebrew);

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
            await state.ChangeAsync(new CultureInfo("en"));

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
            var change = state.ChangeAsync(German, Hebrew);

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
        public async Task Should_reject_a_missing_culture()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var state = GetState(scope);

            // Act
            var exception = await Record.ExceptionAsync(() => state.ChangeAsync(null!));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
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
            await state.ChangeAsync(German, Hebrew);

            // Assert
            Assert.Equal(["de-DE/he r1"], notifiedCultures);
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
            var change = state.ChangeAsync(Hebrew);
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
            await state.ChangeAsync(Hebrew);

            // Act
            state.Unsubscribe(CountAsync);
            await state.ChangeAsync(English);

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
            await GetState(scope).ChangeAsync(Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(English, GetState(otherScope).Culture),
                () => Assert.Equal(0, GetState(otherScope).Revision),
                () => Assert.Equal(0, otherNotifications));
        }
    }
}
