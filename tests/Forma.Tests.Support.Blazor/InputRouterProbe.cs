namespace Forma.Tests.Blazor;

using Bunit;

using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Stands in for the input router's script under a bUnit context: records what components register, and presses a key the way the router would, on the registration that declares the action.</summary>
public sealed class InputRouterProbe
{
    private readonly BunitContext _context;
    private readonly BunitJSModuleInterop _module;

    /// <summary>Sets the router's module up on the context's interop, before anything renders.</summary>
    public InputRouterProbe(BunitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _module = context.JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
    }

    /// <summary>The router's module, for a test that answers one of its calls itself.</summary>
    public BunitJSModuleInterop Module
        => _module;

    /// <summary>Every registration made so far, in order.</summary>
    public IReadOnlyList<JSRuntimeInvocation> Registrations
        => [.. _module.Invocations.Where(invocation => invocation.Identifier == "register")];

    /// <summary>The routes a registration declares, each as <c>action:key,key</c>, a typing binding written as <c>typing</c>.</summary>
    public static IReadOnlyList<string> GetRoutes(JSRuntimeInvocation registration)
        => [.. GetProfile(registration).InputRoutes
            .Select(route => $"{route.Action}:{string.Join(',', route.Bindings.Select(Describe))}")];

    /// <summary>Presses <paramref name="key"/> as <paramref name="action"/> on the last registration that declares the action, on the renderer.</summary>
    public Task RouteAsync(string action, string key)
        => RouteAsync(Registrations.Last(registration => GetProfile(registration).InputRoutes.Any(route => route.Action == action)), action, key);

    /// <summary>Presses <paramref name="key"/> as <paramref name="action"/> on the registration made on <paramref name="element"/>, on the renderer.</summary>
    public Task RouteAsync(ElementReference element, string action, string key)
        => RouteAsync(Registrations.Last(registration => ((ElementReference)registration.Arguments[0]!).Id == element.Id), action, key);

    /// <summary>Delivers a dismissal to the registration made on <paramref name="element"/>, on the renderer.</summary>
    public Task DismissAsync(ElementReference element, DismissBehavior action, DismissReason reason)
        => _context.Renderer.Dispatcher.InvokeAsync(() => GetOwner(Registrations.Last(registration => ((ElementReference)registration.Arguments[0]!).Id == element.Id)).HandleDismissAsync(action, reason));

    private Task RouteAsync(JSRuntimeInvocation registration, string action, string key)
        => _context.Renderer.Dispatcher.InvokeAsync(() => GetOwner(registration).HandleInputActionAsync(action, new InputKeyInfo { Type = "keydown", Key = key }));

    private static string Describe(InputBinding binding)
        => binding switch
        {
            KeyBinding key => key.Key,
            TypingBinding => "typing",
            _ => binding.Type,
        };

    private static InputRegistration GetOwner(JSRuntimeInvocation registration)
        => ((DotNetObjectReference<InputRegistration>)registration.Arguments[2]!).Value;

    private static InputProfile GetProfile(JSRuntimeInvocation registration)
        => (InputProfile)registration.Arguments[1]!;
}
