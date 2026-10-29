# Forma WebAssembly Design

Forma's components run under every interactive render mode a Blazor Web App offers, Interactive Server,
Interactive WebAssembly and Interactive Auto, and in a standalone WebAssembly app. This design states what
a WebAssembly client must be able to load, how a host registers Forma on each side of a Web App, what a
client's build keeps, how strings reach the browser, and how the culture crosses from the server to the
client. The culture's own contract, with the switch and the coordinator every runtime on a page shares, is
the [culture design](culture-design.md)'s; the steps that build this design are the
[implementation plan](webassembly-implementation-plan.md)'s.

## The contract

A component behaves alike in every runtime: it renders, binds, reaches the browser through its interop,
shows its strings and follows the culture. A host registers Forma once in each project that renders, and
a published, trimmed WebAssembly app behaves as its untrimmed build does.

## What a client loads

A WebAssembly app runs only assemblies built for the browser, and the shared framework,
`Microsoft.AspNetCore.App`, has no runtime pack for it. So every library a client can load references the
packages it compiles against, `Microsoft.JSInterop`, `Microsoft.AspNetCore.Components`,
`Microsoft.AspNetCore.Components.Web`, `Microsoft.AspNetCore.Components.Authorization` or
`Microsoft.AspNetCore.WebUtilities`, and the shared framework stays with the halves that serve the server
alone: the circuit integrations and the culture router.

Dependencies point from the server to the client, never back. The components assembly references no
server half, and what only a server can do lives in the product's server half: the circuit integrations'
registration, and `FormaPageHead`, which renders the server's document head through the server's
endpoints. What only WebAssembly does lives in the product's WebAssembly half. Each half takes its render
mode's name, as the culture's halves do:

| Assembly | Holds |
| --- | --- |
| `Forma.Blazor` | The components and the render-mode-neutral registrar |
| `Forma.Blazor.Server` | `FormaPageHead` and the server's registrar, which adds the Framework's circuit halves |
| `Forma.Blazor.WebAssembly` | The client's registrar, which adds the Framework's WebAssembly halves |

## Registration

A host makes one call on each side, and still overrides any single service by registering its own first:

- **The client** calls the WebAssembly half's registrar, which registers what the neutral registrar
  does and the WebAssembly halves: the culture's startup, which registers the runtime's state with the
  page's coordinator.
- **The server** calls the server half's registrar, which registers what the neutral registrar does and
  the circuit halves. A server prerenders every client component, so it registers everything a client
  component resolves, whether or not it renders interactive server components itself.
- **The neutral registrar** registers every service a component resolves in any runtime, the culture
  state and the switch among them, and nothing that needs a circuit or a WebAssembly runtime. A
  component never fails to render for want of a service its render mode's half registers.

## The client's build

- **Runtime async** stays off for every assembly a client loads: WebAssembly's runtime throws
  `InvalidProgramException` for an async method compiled with the feature. It returns when that runtime
  supports it, which the browser check in the gate decides.
- **Trimming.** Every library a client loads is trim-compatible and declares itself `IsTrimmable`, with
  the trim analyzer on in its build, so a trim warning fails the build as any warning does. What reaches
  reflection carries the annotation its target declares: a generic value passed to interop or to binding
  carries the members the target requires, JSON is read and written through source-generated contexts,
  and a component type rendered by its type, through `DynamicComponent` among others, or a plugin type
  created through its constructor, is annotated where it enters.
- **Locale packs** reach a trimmed client because the host's build records each pack it references in
  the host's assembly, the package's own targets doing what the repository's build does for its hosts.

## Strings in the browser

A string is looked up synchronously in every runtime, so each locale file a lookup reads is in the client
before the first lookup. The library's packs are embedded in their assemblies. A host's own locale files
reach the client before its first lookup too, without a file system to read them from, so a host's string
switches with the library's.

## The culture across the handoff

A prerendered WebAssembly component starts in the pair the server rendered it in, so its first interactive
render matches its prerender and the page never flips language as it hydrates:

- **The server hands its pair to the client.** The framework persists the request's culture and starts the
  client in it where capture is on, `Components:UseCultureFromServer`: an explicit value decides, and
  without one capture is on only where `IStringLocalizerFactory` is registered. Forma's server registrar
  turns capture on, so a host doesn't name the setting, and a host's explicit `false` stays its choice.
- **The client's state starts in it** with no work of its own, since the state reads the ambient pair when
  the runtime's scope first resolves it, after the framework has applied the handed pair.
- **Registration settles what the handoff missed.** The runtime's state registers with the page's
  coordinator as the runtime starts and adopts the accepted pair, so a client that started in another
  pair, with capture off, converges after its first render.
- **The process default is WebAssembly's alone.** Applying an accepted pair under WebAssembly also sets
  the process's default cultures, so each later event starts in it, as the culture design's output
  section sets out; a server never sets them.

## Decisions

| | Question | Decision |
| --- | --- | --- |
| W1 | What does a client library reference? | The packages it compiles against, never the shared framework, which has no browser runtime pack |
| W2 | Where do the server-only and WebAssembly-only parts live? | In the product's halves, `Forma.Blazor.Server` and `Forma.Blazor.WebAssembly`, each with its own registrar, so the components assembly references no server half |
| W3 | Does a client compile with runtime async? | No, until WebAssembly's runtime supports it |
| W4 | Are the client libraries trim-compatible? | Yes, declared `IsTrimmable` with the analyzer on, reflection annotated where it enters and JSON source-generated |
| W5 | How does a prerendered client start in the server's culture? | Through the framework's capture, which Forma's server registrar turns on, and its state's registration with the coordinator, which settles a client that started elsewhere |
| W6 | Who sets the process's default cultures? | WebAssembly's culture half, as it applies each accepted pair; never a server |
