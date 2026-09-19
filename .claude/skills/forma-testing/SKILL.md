---
name: forma-testing
description: The test standard — the one-test-project-per-assembly rule and where a contracts assembly is tested, the bUnit + xUnit v3 file shape with a nested class per subject, naming and Arrange/Act/Assert conventions, rendering form inputs in a fake form host, the shared support project every fake lives in, driving interop wrappers through the fake runtime, and the separate Vitest runner for script modules. Use when writing or changing any test, adding a test fake, or testing a JS behavior, operation, component or service module.
---

# Tests (bUnit + xUnit v3)

Every production assembly has exactly one test project named after it, and is never tested from another project. A contracts assembly whose behavior only shows through what extends it is the exception: it is exercised through the test project of what extends it. Mirror source paths within a test project, so a component's tests live in the category folder matching the component's, as `FormaXTests.razor` with `FormaXTests.razor.cs`.

- Do keep `.razor.cs` to `public sealed partial class FormaXTests;`.
- Do start `.razor` with `@inherits` naming the shared bUnit context and place `[Fact]` methods in a nested `public sealed class Component`; the context registers localization, the bundled artwork and the aggregate registrar, so a test class registers nothing of its own for those.
- Do register a `StubStringLocaleFactory` or a `FakeLoggerFactory` from the support project after the context is made, with `AddSingleton`, when a test must observe the strings a component asked for or what it logged.
- Do name tests like `Should_render_checked_when_value_is_true` and use explicit `// Arrange`, `// Act`, and `// Assert` comments.
- Do put the subject a test is about on a nested class named for it, and start the method with `Should_`: `ManagerTests.Registration.Should_place_each_new_window_at_the_front`, `PluginTests.Typography.Should_dispatch_typography_commands`, `ShareTests.Dropbox.Should_ask_the_api_to_save_the_resources_address`. A nested class is named for the concept, without a `Plugin` or `Share` suffix that would shadow the production type it exercises.
- Don't prefix the subject onto the method name (`TypographyPlugin_should_dispatch_typography_commands`, `Dropbox_should_ask_…`) or write a test name in PascalCase (`RegistersTheDefaultInputPolicy`).
- Do name a test helper by the same rules as any method: `CreateItems()`, `CreateRouter()`, `GetTitles(windows)`, `GetSelector(window)`, never a bare noun such as `Items()` or `Router()`.
- Do point `[MemberData]` at the outer class with `MemberType = typeof(OuterTests)` when the source lives there and the test sits in a nested class.
- Do create the context with `using var ctx = new FormaXTests();` and render with `ctx.Render(@<FormaX ... />)`.
- Do render form inputs inside `FakeFormHost` with a `FakeModel` and `EditContext`.
- Do assert rendered markup, attributes, and callback invocations rather than private state.
- Do assert exact text when a component can render more than one message through the same element or class, such as an empty-list notice versus a no-search-matches notice; finding the element only confirms *a* message rendered, not which one.
- Do put a service's tests beside the category that owns it, because the test path mirrors where the production type lives.
- Do put every test fake, fixture, and utility in the shared support project, in a folder mirroring the production area it serves, and put what no single area owns in `Shared/`.
- Do put what needs bUnit or the components assembly, such as the shared bUnit context, in the bUnit support project beside it, which only the component test projects reference, so a Framework test never restores bUnit or the product.
- Do drive an interop wrapper in a test through `FakeJSRuntime` and `FakeJSObjectReference` from the support project's interop folder, wrapped in a `JSEnvironment` over a `FakeLoggerFactory` logger, so the test reads what the wrapper returned and what it logged.
- Do let the namespace follow the support folder, and reference the support project from each test project that uses one.
- Do test only the public surface and keep shared usings in `_Imports.cs` or `_Imports.razor`.
- Don't test a type from a project other than the one named after its assembly, or — for an `.Abstractions` assembly — the one named after its sibling.
- Don't couple tests to implementation details or private fields.
- Don't give a test project its own `Support/` folder or its own copy of a support type.

## What bUnit cannot see

bUnit renders markup and drives its own synchronous JS interop, so a green suite is not evidence about CSS, about pointer behavior, or about anything whose timing depends on a real interop round-trip.

- Do verify stylesheet-dependent states, touch targets, gestures, and direction in a real browser instead — the `forma-checking-a11y-touch-rtl` skill covers how.
- Don't read a passing bUnit test as proof that the component works in the showcase.

## Time and completion

A test that passes alone and fails under load is broken, not unlucky: its assertion depends on how quickly a continuation is scheduled. Load only exposes the race.

- Do hold a component's asynchronous work in a fake the test controls, and let it go through `cut.ReleaseAsync(...)` from the bUnit support project, which runs the release on the renderer; the component's continuation then runs there at once, so the test asserts directly after.
- Do give such a fake a plain `TaskCompletionSource`; one created to run its continuations asynchronously queues the component's continuation instead of running it inline, and the assertion races it again.
- Do await the handler's own task, from `ClickAsync` or `TriggerEventAsync`, where the component's work passes through a library that awaits with `ConfigureAwait(false)`; that work leaves the renderer, and nothing done on the renderer can wait for it.
- Do give a component a contract to inject, as `IEmojiSpriteLoader` beside `EmojiSpriteLoader`, when a test needs to hold its work and the only way in was a pending script call.
- Don't wait on the clock for a completion the test controls: no `WaitForAssertion` or `WaitForState` after completing a gate, no `Task.Delay`, and never a widened timeout or a retry, which hide the race rather than remove it.
- Do prove a fix under the conditions that failed, repeating the project while something else loads the machine, before calling the test stable.

## JavaScript tests

The script test project runs the `wwwroot/scripts/` modules of every assembly that ships scripts under Vitest and happy-dom. It reaches each assembly through an alias named for it and holds that assembly's tests in a folder of the same name, laid out as the assembly's `wwwroot/scripts/` is.

- Do add a `{Name}.test.js` beside its siblings whenever you add or change a behavior, operation, component or service module.
- Do drive it with `npm test` from that project; it is a separate runner and `dotnet test` does not reach it.
- Do exercise `dispose()` in a behavior's tests, since a leaked listener is exactly what the handle contract exists to prevent.
- Don't read the no-npm rule as covering this project; that rule governs what the library ships to a browser, not the dev-time test runner.
