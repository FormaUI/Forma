---
name: forma-tests
description: Forma's test standard — the one-test-project-per-assembly rule and where an .Abstractions assembly is tested, the bUnit + xUnit v3 file shape (FormaXTests.razor with a nested Component class), naming and Arrange/Act/Assert conventions, rendering InputBase components in FakeFormHost, where shared fakes live in Forma.Tests.Support, and the separate Vitest runner for wwwroot/scripts modules. Use when writing or changing any test, adding a test fake, or testing a JS behaviour, operation, or component module.
---

# Tests (bUnit + xUnit v3)

Every production assembly has exactly one test project named after it, so `Forma.UI.Design.Icons` is tested by `tests/Forma.UI.Design.Icons.Tests` and never from another project. An `.Abstractions` assembly is the exception: it shares its sibling's namespace, so it shares its sibling's test project too — `Forma.UI.Features.Abstractions` is tested from `tests/Forma.UI.Features.Tests`. Mirror source paths within it, so component tests live under `tests/Forma.UI.Blazor.Tests/Components/{Category}/` with `FormaXTests.razor` and `FormaXTests.razor.cs`.

- Do keep `.razor.cs` to `public sealed partial class FormaXTests;`.
- Do start `.razor` with `@inherits BunitContext` and place `[Fact]` methods in a nested `public sealed class Component`.
- Do name tests like `Should_render_checked_when_value_is_true` and use explicit `// Arrange`, `// Act`, and `// Assert` comments.
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

- Do verify stylesheet-dependent states, touch targets, gestures, and direction in a real browser instead — the `forma-a11y-touch-rtl` skill covers how.
- Don't read a passing bUnit test as proof that the component works in the Showcase.

## JavaScript tests

`tests/Forma.UI.Blazor.Tests.Scripts` runs the `wwwroot/scripts/` modules under Vitest and happy-dom, mirroring the source layout in its own `behaviours/`, `operations/`, and `components/` folders.

- Do add a `{Name}.test.js` beside its siblings whenever you add or change a behaviour, operation, or component module.
- Do drive it with `npm test` from that folder; it is a separate runner and `dotnet test` does not reach it.
- Do exercise `dispose()` in a behaviour's tests, since a leaked listener is exactly what the handle contract exists to prevent.
- Don't read the no-npm rule as covering this project; that rule governs what the library ships to a browser, not the dev-time test runner.
