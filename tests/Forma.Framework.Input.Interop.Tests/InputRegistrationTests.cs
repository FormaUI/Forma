namespace Forma.Framework.Input.Interop.Tests;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Input.Keyboard;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Xunit;

public sealed class InputRegistrationTests
{
    private static readonly InputProfile Profile = new() { InputRoutes = [new InputRoute("test.next", new KeyBinding("ArrowDown"))] };
    private static readonly InputProfile OtherProfile = new() { InputRoutes = [new InputRoute("test.previous", new KeyBinding("ArrowUp"))] };
    private static readonly ElementReference Element = new("element");

    public sealed class Registering
    {
        [Fact]
        public async Task Should_register_the_element_once_wanted()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript();
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);

            // Act
            await registration.SyncAsync(Element, Profile);

            // Assert
            var call = Assert.Single(script.Module.Calls, call => call.Identifier == "register");
            Assert.Multiple(
                () => Assert.True(registration.IsRegistered),
                () => Assert.Same(Profile, call.Arguments![1]),
                () => Assert.Same(registration, Assert.IsType<DotNetObjectReference<InputRegistration>>(call.Arguments![2]).Value));
        }

        [Fact]
        public async Task Should_leave_an_unrendered_element_alone()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript();
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);

            // Act
            await registration.SyncAsync(default, Profile);

            // Assert
            Assert.False(registration.IsRegistered);
            Assert.DoesNotContain(script.Module.Calls, call => call.Identifier == "register");
        }

        [Fact]
        public async Task Should_release_the_element_once_no_longer_wanted()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript();
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);
            await registration.SyncAsync(Element, Profile);

            // Act
            await registration.SyncAsync(Element, Profile, wanted: false);

            // Assert
            Assert.False(registration.IsRegistered);
            Assert.Contains(script.Handle.Calls, call => call.Identifier == "dispose");
        }

        [Fact]
        public async Task Should_start_afresh_on_another_element()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript();
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);
            await registration.SyncAsync(Element, Profile);

            // Act
            await registration.SyncAsync(new ElementReference("other"), Profile);

            // Assert
            var registrations = script.Module.Calls.Where(call => call.Identifier == "register").ToList();
            Assert.Multiple(
                () => Assert.Equal(2, registrations.Count),
                () => Assert.Equal("other", Assert.IsType<ElementReference>(registrations[1].Arguments![0]).Id),
                () => Assert.Contains(script.Handle.Calls, call => call.Identifier == "dispose"));
        }

        [Fact]
        public async Task Should_start_afresh_when_the_elements_counted_inside_change()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript();
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);
            await registration.SyncAsync(Element, Profile, within: [new ElementReference("first-trigger")]);

            // Act — the same trigger again through another reference, then another trigger
            await registration.SyncAsync(Element, Profile, within: [new ElementReference("first-trigger")]);
            await registration.SyncAsync(Element, Profile, within: [new ElementReference("second-trigger")]);

            // Assert
            var registrations = script.Module.Calls.Where(call => call.Identifier == "register").ToList();
            Assert.Multiple(
                () => Assert.Equal(2, registrations.Count),
                () => Assert.Single(script.Handle.Calls, call => call.Identifier == "dispose"));
        }

        [Fact]
        public async Task Should_ask_again_on_the_next_sync_after_the_router_refused()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript(refuses: true);
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);
            await registration.SyncAsync(Element, Profile);

            // Act
            await registration.SyncAsync(Element, Profile);

            // Assert
            Assert.False(registration.IsRegistered);
            Assert.Equal(2, script.Module.Calls.Count(call => call.Identifier == "register"));
        }
    }

    public sealed class KeepingInStep
    {
        [Fact]
        public async Task Should_push_a_profile_that_changed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript();
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);
            await registration.SyncAsync(Element, Profile);

            // Act
            await registration.SyncAsync(Element, Profile);
            await registration.SyncAsync(Element, OtherProfile);

            // Assert
            var update = Assert.Single(script.Handle.Calls, call => call.Identifier == "update");
            Assert.Same(OtherProfile, update.Arguments![0]);
        }

        [Fact]
        public async Task Should_hold_the_registration_active_while_asked_and_let_go_after()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript();
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);

            // Act
            await registration.SyncAsync(Element, Profile, active: true);
            var heldActive = registration.IsActive;
            await registration.SyncAsync(Element, Profile, active: false);

            // Assert
            Assert.Multiple(
                () => Assert.True(heldActive),
                () => Assert.False(registration.IsActive),
                () => Assert.Equal(["activate", "release"], script.Handle.Calls.Select(call => call.Identifier).Where(identifier => identifier is "activate" or "release")));
        }
    }

    public sealed class InFlight
    {
        [Fact]
        public async Task Should_release_a_registration_that_lands_after_the_element_stopped_being_wanted()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript(holdsRegister: true);
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);
            var pending = registration.SyncAsync(Element, Profile);

            // Act
            await registration.SyncAsync(Element, Profile, wanted: false);
            script.Held!.Answer(script.Handle);
            await pending;

            // Assert
            Assert.False(registration.IsRegistered);
            Assert.Contains(script.Handle.Calls, call => call.Identifier == "dispose");
        }

        [Fact]
        public async Task Should_apply_a_profile_that_changed_while_the_registration_was_in_flight()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript(holdsRegister: true);
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);
            var pending = registration.SyncAsync(Element, Profile);

            // Act
            await registration.SyncAsync(Element, OtherProfile);
            script.Held!.Answer(script.Handle);
            await pending;

            // Assert
            var update = Assert.Single(script.Handle.Calls, call => call.Identifier == "update");
            Assert.Same(OtherProfile, update.Arguments![0]);
        }

        [Fact]
        public async Task Should_register_once_when_syncs_overlap()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript(holdsRegister: true);
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(router);
            var first = registration.SyncAsync(Element, Profile);

            // Act
            await registration.SyncAsync(Element, Profile);
            script.Held!.Answer(script.Handle);
            await first;

            // Assert
            Assert.True(registration.IsRegistered);
            Assert.Equal(1, script.Module.Calls.Count(call => call.Identifier == "register"));
        }

        [Fact]
        public async Task Should_wait_for_a_registration_in_flight_before_disposing()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript(holdsRegister: true);
            await using var router = CreateRouter(script, loggerFactory);
            var registration = new InputRegistration(router);
            var pending = registration.SyncAsync(Element, Profile);

            // Act
            var disposal = registration.DisposeAsync();
            var settledEarly = disposal.IsCompleted;
            script.Held!.Answer(script.Handle);
            await pending;
            await disposal;

            // Assert
            Assert.Multiple(
                () => Assert.False(settledEarly),
                () => Assert.False(registration.IsRegistered),
                () => Assert.Contains(script.Handle.Calls, call => call.Identifier == "dispose"));
        }
    }

    public sealed class Owning
    {
        [Fact]
        public async Task Should_forward_actions_and_dismissals_to_its_owner()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var script = new RecordingScript();
            var actions = new List<string>();
            var dismissals = new List<DismissReason>();
            await using var router = CreateRouter(script, loggerFactory);
            await using var registration = new InputRegistration(
                router,
                (action, _) => { actions.Add(action); return Task.CompletedTask; },
                (_, reason) => { dismissals.Add(reason); return Task.CompletedTask; });

            // Act
            await registration.HandleInputActionAsync("test.next", new InputKeyInfo { Type = "keydown", Key = "ArrowDown" });
            await registration.HandleDismissAsync(DismissBehavior.Dismiss, DismissReason.Escape);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["test.next"], actions),
                () => Assert.Equal([DismissReason.Escape], dismissals));
        }
    }

    private static InputRouter CreateRouter(RecordingScript script, FakeLoggerFactory loggerFactory)
        => new(new FakeJSRuntime((identifier, _) => identifier == "import" ? script.Module : throw new JSException($"'{identifier}' is not an import.")), loggerFactory.CreateLogger<InputRouter>(), new InputRouterOptions());

    /// <summary>The router's script as the tests see it: a module answering configure and register, and the handle register hands back, each recording its calls.</summary>
    private sealed class RecordingScript
    {
        public RecordingScript(bool refuses = false, bool holdsRegister = false)
        {
            Handle = new RecordingHandle();
            Held = holdsRegister ? new HeldJSObjectReference("register", (_, _) => null) : null;
            Module = new RecordingModule(
                identifier => identifier switch
                {
                    "register" when refuses => throw new JSException("refused"),
                    "register" => Handle,
                    _ => null,
                },
                Held);
        }

        public RecordingModule Module { get; }

        public RecordingHandle Handle { get; }

        public HeldJSObjectReference? Held { get; }
    }

    private sealed record Call(string Identifier, object?[]? Arguments);

    private sealed class RecordingModule(Func<string, object?> respond, HeldJSObjectReference? held) : IJSObjectReference
    {
        public List<Call> Calls { get; } = [];

        [SuppressMessage("Design", "CA1031", Justification = "The call faults with whatever the function throws, as a runtime call does.")]
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(new Call(identifier, args));

            if (held is not null && identifier == "register")
            {
                return held.InvokeAsync<TValue>(identifier, args);
            }

            try
            {
                return ValueTask.FromResult((TValue)respond(identifier)!);
            }
            catch (Exception ex)
            {
                return ValueTask.FromException<TValue>(ex);
            }
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private sealed class RecordingHandle : IJSObjectReference
    {
        public List<Call> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(new Call(identifier, args));

            return ValueTask.FromResult<TValue>(default!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
