---
name: forma-testing
description: Forma's test standard — the one-test-project-per-assembly rule and where a split assembly's contracts are tested, the bUnit + xUnit v3 file shape (FormaXTests.razor with a nested Component class), naming and Arrange/Act/Assert conventions, rendering InputBase components in FakeFormHost, where shared fakes live in Forma.Tests.Support, and the separate Vitest runner for wwwroot/scripts modules. Use when writing or changing any test, adding a test fake, or testing a JS behaviour, operation, or component module.
---

# Tests (bUnit + xUnit v3)

Every production assembly has exactly one test project named after it, so `Forma.Framework.Icons` is tested by `tests/Forma.Framework.Icons.Tests` and never from another project. A Framework contracts assembly whose behavior only shows through what extends it is the exception: `Forma.Framework.Share` is exercised through `tests/Forma.AddOns.Share.Targets.Tests`. Mirror source paths within it, so component tests live under `tests/Forma.Blazor.Tests/Components/{Category}/` with `FormaXTests.razor` and `FormaXTests.razor.cs`.

- Do keep `.razor.cs` to `public sealed partial class FormaXTests;`.
- Do start `.razor` with `@inherits FormaBunitContext` and place `[Fact]` methods in a nested `public sealed class Component`; the context registers localization, the bundled artwork and `AddFormaUI`, so a test class registers nothing of its own for those.
- Do register a `StubStringLocaleFactory` or a `FakeLoggerFactory` from `Forma.Tests.Support` after the context is made, with `AddSingleton`, when a test must observe the strings a component asked for or what it logged.
- Do name tests like `Should_render_checked_when_value_is_true` and use explicit `// Arrange`, `// Act`, and `// Assert` comments.
- Do put the subject a test is about on a nested class named for it, and start the method with `Should_`: `WindowManagerTests.Registration.Should_place_each_new_window_at_the_front`, `RichTextPluginTests.Typography.Should_dispatch_typography_commands`, `CloudShareTests.Dropbox.Should_ask_the_api_to_save_the_resources_address`. A nested class is named for the concept, without a `Plugin` or `Share` suffix that would shadow the production type it exercises.
- Don't prefix the subject onto the method name (`TypographyPlugin_should_dispatch_typography_commands`, `Dropbox_should_ask_…`) or write a test name in PascalCase (`RegistersTheDefaultInputPolicy`).
- Do name a test helper by the same rules as any method: `CreateItems()`, `CreateRouter()`, `GetTitles(windows)`, `GetSelector(window)`, never a bare noun such as `Items()` or `Router()`.
- Do point `[MemberData]` at the outer class with `MemberType = typeof(OuterTests)` when the source lives there and the test sits in a nested class.
- Do create the context with `using var ctx = new FormaXTests();` and render with `ctx.Render(@<FormaX ... />)`.
- Do render `InputBase<T>` components inside `FakeFormHost` with a `FakeModel` and `EditContext`.
- Do assert rendered markup, attributes, and callback invocations rather than private state.
- Do assert exact text when a component can render more than one message through the same element or class, such as an empty-list notice versus a no-search-matches notice; finding the element only confirms *a* message rendered, not which one.
- Do put a service's tests beside the category that owns it, such as `Feedback/ToastServiceTests.cs`, because the test path mirrors where the production type lives.
- Do put every test fake, fixture, and utility in `tests/Forma.Tests.Support`, in a folder mirroring the production area it serves such as `Resources/`, `Media/`, and `Office/`, and put what no single area owns in `Shared/`.
- Do let the namespace follow that folder, so `Forma.Tests.Resources` and `Forma.Tests.Shared`, and reference the project from each test project that uses one.
- Do test only the public surface and keep shared usings in `_Imports.cs` or `_Imports.razor`.
- Don't test a type from a project other than the one named after its assembly, or — for an `.Abstractions` assembly — the one named after its sibling.
- Don't couple tests to implementation details or private fields.
- Don't give a test project its own `Support/` folder or its own copy of a support type.

## What bUnit cannot see

bUnit renders markup and drives its own synchronous JS interop, so a green suite is not evidence about CSS, about pointer behaviour, or about anything whose timing depends on a real interop round-trip.

- Do verify stylesheet-dependent states, touch targets, gestures, and direction in a real browser instead — the `forma-checking-a11y-touch-rtl` skill covers how.
- Don't read a passing bUnit test as proof that the component works in the Showcase.

## JavaScript tests

`tests/Forma.Blazor.Tests.Scripts` runs the `wwwroot/scripts/` modules of every assembly that ships scripts under Vitest and happy-dom. It reaches each assembly through an alias named for it, `#core/`, `#common/`, `#input/` and `#composer/`, and holds that assembly's tests in a folder of the same name, `core/`, `common/`, `input/` and `composer/`, laid out as the assembly's `wwwroot/scripts/` is.

- Do add a `{Name}.test.js` beside its siblings whenever you add or change a behaviour, operation, or component module.
- Do drive it with `npm test` from that folder; it is a separate runner and `dotnet test` does not reach it.
- Do exercise `dispose()` in a behaviour's tests, since a leaked listener is exactly what the handle contract exists to prevent.
- Don't read the no-npm rule as covering this project; that rule governs what the library ships to a browser, not the dev-time test runner.
