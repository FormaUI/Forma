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
`Microsoft.AspNetCore.Components.Forms`, `Microsoft.AspNetCore.Components.Web` or
`Microsoft.AspNetCore.WebUtilities`, and the shared framework stays with the halves that serve the server
alone: the product's server half, the circuit integrations and the culture router's middleware.

Dependencies point from the server to the client, never back. The components assembly references no
server half, and what only a server can do lives in the product's server half: the circuit integrations'
registration, and `FormaPageHead`, which renders the server's document head through the server's
endpoints. What only WebAssembly does lives in the product's WebAssembly half. Each half takes its render
mode's name, as the culture's halves do:

| Assembly | Holds |
| --- | --- |
| `Forma.Blazor` | The components and the render-mode-neutral registrar, `AddFormaUI` |
| `Forma.Blazor.Server` | `FormaPageHead` and the server's registrar, `AddFormaUIServer`, which adds the Framework's circuit halves |
| `Forma.Blazor.WebAssembly` | The client's registrar, `AddFormaUIWebAssembly`, which adds the Framework's WebAssembly halves |

## Registration

A host makes one call on each side, and still overrides any single service by registering its own first:

- **The client** calls `AddFormaUIWebAssembly`, which registers what `AddFormaUI` does and the WebAssembly
  halves: the culture's startup, which registers the runtime's state with the page's coordinator under
  coordinated pages, or sets up the local switch under an independent client culture.
- **The server** calls `AddFormaUIServer` on its builder, which registers what `AddFormaUI` does and the
  circuit halves. A server prerenders every client component, so it registers everything a client
  component resolves, whether or not it renders interactive server components itself.
- **`AddFormaUI`** registers every service a component resolves in any runtime, the culture state among
  them, and nothing that needs a circuit or a WebAssembly runtime; each render mode's half registers the
  switch that requests through the page's coordinator, or the local switch where its host chose an
  independent client culture. A component never fails
  to render for want of a service its render mode's half registers.

The culture's own registrars follow the same pattern, `AddFormaCultureServer` and
`AddFormaCultureWebAssembly`, which the product's halves call.

A server that renders WebAssembly components chooses how its pages treat a client's culture through the
culture arrangement, an option on `AddFormaUIServer` and on `AddFormaUIWebAssembly`, coordinated unless a
host declares it otherwise. Each arrangement is complete on both sides:

| Arrangement | The server | The client | The page |
| --- | --- | --- | --- |
| Coordinated, the default | `AddFormaUIServer` with coordinated pages; `FormaPageHead` in the document's head; capture on, which the registrar supplies where the host sets nothing | `AddFormaUIWebAssembly`, whose runtime registers with the page's coordinator | Starts from the server's resolved pair, and every runtime on it follows the coordinator |
| Independent client culture | `AddFormaUIServer` declaring the client's culture independent; capture may be off | `AddFormaUIWebAssembly` declaring the same, with the local switch | The server's runtimes follow the coordinator; the client's culture is the client's own, and touches neither the document, the coordinator, the address nor the cookie |

```csharp
// Coordinated: the server's Program.cs, then the client's.
builder.AddFormaUIServer();
builder.Services.AddFormaUIWebAssembly();

// Independent client culture: the same two calls, each declaring it.
builder.AddFormaUIServer(options => options.CultureArrangement = CultureArrangement.Independent);
builder.Services.AddFormaUIWebAssembly(options => options.CultureArrangement = CultureArrangement.Independent);
```

A coordinated prerendered page needs the server's initialization, so an explicit `false` for
`Components:UseCultureFromServer` beside coordinated pages is an unsupported combination, which Forma
rejects rather than reinterprets. The check reads the final configuration at startup, once every
registration is made, so the order of the registrations doesn't change its result, and it runs only
where the server renders WebAssembly components under coordinated pages: installing the server half, or
a server that renders no WebAssembly, checks nothing. It throws `InvalidOperationException` naming the
setting, the coordinated arrangement and the two ways out, removing the `false` or declaring the
client's culture independent. A standalone WebAssembly app has no server and initializes its coordinator
from its own startup, which supplies the initial pair and the switch's policy.

## The client's build

- **Runtime async** stays off for every assembly a client loads: WebAssembly's runtime throws
  `InvalidProgramException` for an async method compiled with the feature.
- **Trimming.** Every library a client loads is trim-compatible and declares itself `IsTrimmable`, with
  the trim analyzer on in its build, so a trim warning fails the build as any warning does. What reaches
  reflection carries the annotation its target declares: a generic value passed to interop or to binding
  carries the members the target requires, JSON is read and written through source-generated contexts,
  and a component type rendered by its type, through a render fragment that opens it, or a plugin type
  created through its constructor, is annotated where it enters.
- **Locale packs** reach a trimmed client because the host's build records each pack it references in
  the host's assembly, the package's own targets doing what the repository's build does for its hosts.

## Strings in the browser

A string is looked up synchronously in every runtime, so each locale file a lookup reads is in the client
before the first lookup. The library's packs are embedded in their assemblies, and a host's own locale
files are embedded in its client assembly the same way, by the package's targets, as the
[localization design](localization-design.md#host-files) sets out, so a host's string switches with the
library's.

## The culture across the handoff

A prerendered WebAssembly component starts in the pair the server rendered it in, so its first interactive
render matches its prerender and the page never flips language as it hydrates:

- **The server hands its pair to the client.** The framework persists the request's culture and starts the
  client in it where capture is on, `Components:UseCultureFromServer`: an explicit value decides, and
  without one capture is on only where `IStringLocalizerFactory` is registered. `AddFormaUIServer`, on the
  host's builder, supplies capture on as a default for coordinated prerendered pages, only where the
  configuration names no value, so a host doesn't name the setting; an explicit value stays
  authoritative, and an explicit `false` under coordinated pages is the conflict
  [registration](#registration) rejects.
- **The client's state starts in it** with no work of its own, since the state reads the ambient pair when
  the runtime's scope first resolves it, after the framework has applied the handed pair.
- **The server publishes what it rendered.** `FormaPageHead` names the resolved pair, both cultures, as
  the document's culture marker, which the page's coordinator starts from, with the switch's policy
  beside it, as the culture design's coordinator section sets out; a coordinated prerendered page
  therefore requires it. Publishing the pair tells the coordinator what the document shows; it gives no
  runtime permission to take it.
- **A disabled capture stays disabled.** The client's registration with the coordinator never stands in
  for a handoff its host turned off, so Forma never restores the server's pair to a client by a second
  route; a host that turns it off chooses the independent arrangement, and one that turns it off under
  coordinated pages is stopped at startup, as [registration](#registration) sets out.
- **The process default is WebAssembly's alone.** Applying an accepted pair under WebAssembly also sets
  the process's default cultures, so each later event starts in it, as the culture design's output
  section sets out; a server never sets them.

## Decisions

| | Question | Decision |
| --- | --- | --- |
| W1 | What does a client library reference? | The packages it compiles against, never the shared framework, which has no browser runtime pack |
| W2 | Where do the server-only and WebAssembly-only parts live? | In the product's halves, `Forma.Blazor.Server` with `AddFormaUIServer` and `Forma.Blazor.WebAssembly` with `AddFormaUIWebAssembly`, beside the neutral `AddFormaUI`, so the components assembly references no server half |
| W3 | Does a client compile with runtime async? | No, since WebAssembly's runtime throws for a method compiled with it |
| W4 | Are the client libraries trim-compatible? | Yes, declared `IsTrimmable` with the analyzer on, reflection annotated where it enters and JSON source-generated |
| W5 | How does a prerendered client start in the server's culture? | Through the framework's capture, which `AddFormaUIServer` supplies as a default only where the configuration names none; `FormaPageHead`'s culture marker tells the coordinator the page's pair, and a capture the host turned off is never restored by another route |
| W6 | Who sets the process's default cultures? | WebAssembly's culture half, as it applies each accepted pair; never a server |
| W7 | What does a server choose for a client's culture? | The culture arrangement, an option on both product registrars: coordinated by default, which needs the server's initialization and rejects an explicit capture `false` at startup, reading the final configuration; or an independent client culture, declared on both sides, with the local switch on the client |
| W8 | How do a host's locale files reach the client? | Embedded in its client assembly by the package's targets, under resource names and discovery metadata the browser's loader finds |
| W9 | Is `FormaPageHead` required? | On a coordinated prerendered page, where it publishes the culture marker and the switch's policy; a standalone app initializes from its startup instead |
