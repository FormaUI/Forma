# Forma Security Design

What Forma accepts from the code that hosts it and from the browser, what it writes into a page from the data it's
given, and the checks that hold it to both. [The implementation plan](security-implementation-plan.md) holds the steps
that carry it out.

## Two kinds of input

Forma takes input from two places, and treats them differently because only one of them can be trusted.

**A host's arguments** come from the code that uses Forma: a component's parameters, and the arguments of a public
member of a service or a type. A wrong one is a programming error, and it throws where it's found:

- A missing or out-of-range argument throws the `ArgumentException` the .NET throw helpers raise,
  `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`,
  `ArgumentOutOfRangeException.ThrowIfNegative` and their siblings, checked in the order the arguments are declared.
- `?? throw new ArgumentNullException(nameof(x))` stands only where a check must be an expression, as a primary
  constructor's field initializer is.
- A hand-written `ArgumentException` stands only for a rule no helper states, and its message says what was expected.
- An argument that is present but can't be used, such as a relative address where an absolute one is needed, throws
  `InvalidOperationException`.
- A component checks its parameters in `OnParametersSet`, as soon as they're set.

The analyzers enforce what they can state: CA1062, CA1510, CA1511, CA1512, CA1513 and CA2208 are errors in the
`.editorconfig` severity table, so a new member is held to the standard as it's added.

**What the browser sends** is untrusted. On the Server anyone can send anything over a circuit, whatever the page's
own scripts would send. Every method a script calls back into .NET, every answer a script gives a call .NET makes,
every text an input parses, and every file a reader uploads is refused, never thrown:

- What can't be used is ignored and logged at `Debug` under the component that received it.
- Nothing a browser sends ends a circuit on the Server or the application in Blazor Hybrid.
- A value the browser sends as JSON is read through a contract of Forma's own, never by the framework as a type, and
  every number in it is a number: a browser's JSON writes no number as text, so a text such as `"Infinity"` is
  refused as one, as a number past what a double holds is.

A script trusts its C# wrapper. The wrapper checks what it hands the script before the call, so a script checks only
what it reads from the page itself: an event, an element, an attribute.

## What Forma writes into a page

Each place Forma writes markup, an address or a style from data it's given is a sink. The sinks are found from the
source, so a test fails on a new one that has no test with hostile input and takes data other than as a typed value.

- **Markup.** Markup written from data, through a `MarkupString` or a script's `innerHTML`, has passed a sanitizer
  first, or is Forma's own markup with every value in it encoded. The sanitizer is the registered one, or for a page a
  resource provider converted, such as a Markdown or an Office document, the provider's own, which keeps what its
  format renders.
- **The sanitizer's floor.** Under any markup policy, a host's own among them, the sanitizer drops an event handler
  attribute and an address that can run script: `javascript:`, `vbscript:`, and a `data:` address of anything but a
  raster image, a video, audio or captions. A policy narrows what's kept and never lets those through.
- **Addresses.** Forma writes an address as one of two kinds, each allowing the schemes that can't run script where
  it's written. An address its kind doesn't allow, `javascript:` and `vbscript:` among them, is written inert: the
  element keeps its text and takes no address, and the refusal is logged at `Debug`. A component writes every address
  from data through its kind.
  - *A link* a reader follows, an `href`, is relative, or its scheme is `http`, `https`, `mailto` or `tel`. A link that
    downloads may also be a `blob:` or `data:` address, which a download saves rather than opens.
  - *A source* an element fetches, a `src` or a `poster`, is relative, or its scheme is `http`, `https` or `blob:`, or
    it's a `data:` address of the element's own kind: a raster image for an image, since an SVG document can carry
    script, a video for a video, audio for audio and captions for a track. A frame's source and an object's `data` are
    never a `data:` address.
- **Styles.** A style Forma writes from data takes it as a typed CSS value, a length, a color or a number Forma
  formats itself, so data can't add a declaration of its own. A script sets a style one property at a time through
  the element's declarations, which the browser parses for that property alone, and a style the sanitizer keeps has
  lost every rule that fetches an address or runs an expression. A host's own style is the host's, as its class is,
  and so is the preview style a host writes for a toolbar's option.
- **The page's own addresses.** What the host configures for the page itself, its base address and its icon, is the
  host's, and a browser runs no script from either.
- **Artwork.** An artwork pack, the sprite sheets a symbol component draws from, is trusted as a stylesheet is: the
  host chose to ship it, so its symbols are adopted as they are.

## Limits

Every size Forma accepts from the browser has a cap, and a check holds it.

- **An upload.** `FormaFileUploader` reads a file only up to its options' `MaxFileSize`, 10 MB unless a host sets
  another. A file the browser says is larger is refused as too large before any of it is read, and a read stops at the
  cap whatever size the browser claimed.
- **A value in pieces.** A value reaching .NET in pieces is read only up to the length its caller names.
- **The framework's own limits**, such as the largest message a circuit accepts, belong to the host. The README of
  each package names those a host sets, and what Forma needs of them.

## Dependencies

A job of the full gate fails on a known vulnerability in any NuGet package the solution restores, transitive ones
included, and in any npm package of the script tests. It runs on every full run, the nightly one among them, so a
vulnerability published against a package that hasn't changed is found the next night. It stands outside the build,
so an advisory published one day doesn't fail unrelated work.

## A scan of the running Showcase

`scripts/Invoke-SecurityScan.ps1` starts the Showcase and runs OWASP ZAP against it in a container, keeping ZAP's report
and the Showcase's own log. ZAP's spider reaches every page in every culture. Requests made for the scan carry a NUL
character and a lone surrogate in a path, a query and a cookie, and a post names no form and carries no antiforgery
token. ZAP's active scan then attacks every query, form field, path segment, header and cookie it found, and its DOM
XSS rule loads every page's address in a browser, but the video player's, whose embedded players load other sites.

A malformed request is refused, never answered with a 500. The scan fails on an alert of Low risk or above, a response
of 500 or above, or an entry at Error or above in the Showcase's log. An Informational alert, such as ZAP's user agent
fuzzer's, reports what a response shows and fails nothing, and so does a kind this section explains:

| Kind | Why it's no defect of Forma |
|------|-----------------------------|
| No Content Security Policy, or one without the directives ZAP asks for (ZAP's rules 10038 and 10055) | The policy is [step 7's](#a-content-security-policy) |
| No anti-clickjacking header on a static file (10020) | Whether a page may be framed is the host's to say |
| No `X-Content-Type-Options` header (10021) | The headers a response carries are the host server's |
| The site served over HTTP alone (10106) | Whether a site is served over HTTPS is the host's to say |
| The culture cookie without `HttpOnly` (10010) | The page's culture coordinator reads and writes it, and it holds a culture's token alone |
| A Unix timestamp disclosed (10096) | It's a number inside the Showcase's sample video |
| The antiforgery service's error for an antiforgery cookie it can't read | The framework logs it at Error and issues a new cookie |
| A Host header no address can be built from, such as an empty one or a port past 65535 | The framework builds each page's address from it and throws, so a host refuses such a request first, as the Showcase does |

## A Content Security Policy

Forma runs under a strict Content Security Policy: no inline script and no `eval`, with `wasm-unsafe-eval` where
WebAssembly runs. The check pages are served under it, so a script or style Forma emits that the policy would block
fails a check. The README states the policy a host can set and what Forma needs it to allow.
