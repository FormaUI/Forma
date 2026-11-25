# Forma Security Implementation Plan

The steps that carry out [the security design](security-design.md), in order, each landing with its tests and its
gate.

## Steps

1. ✅ **The argument validation standard.** The design's two kinds of input written into the skills as the rule for
   every new member, CA1511 and CA1512 added to the `.editorconfig` severity table, and the codebase brought to the
   standard in one pass: the hand-written argument exceptions, the components that check no parameters, and every
   public member of a service or a type.
   - The two analyzers found nothing to change. Of the 93 hand-written argument exceptions, all but five state a rule
     no helper does; those five, an earliest value after the latest, now throw through
     `ArgumentOutOfRangeException.ThrowIfGreaterThan`, naming the earliest.
   - Of the 67 numeric parameters a component takes, the host-set ones that carried an unchecked constraint are
     checked now: the explorer's totals and page size, the splitter's pixels per unit, the cards' size and the date
     and time picker's offset. A value the reader changes, a current page or a splitter's position, is clamped, as the
     authoring standard now says.
   - The numeric arguments of public members either clamp by design, as `ComposerText` does, fail through the .NET
     member they index, as `UploadBatch` does, or are checked where they're used, as `CssLength` checks that a length
     is finite; none needed a change. What a script sends back through a `[JSInvokable]` method is step 3's.
2. ✅ **What Forma writes, tested against hostile input.** The test that finds every sink from the source, a test of each
   with hostile input, a `javascript:` link and a script element among them, and every sink brought to the design:
   markup sanitized or encoded, and an address outside the design's schemes written inert.
   - One rule for both kinds, `AddressPolicy` and `AddressKind` in `Forma.Framework.Sanitization`, reading an address
     as the browser does; the rich content policy, the file player and the composer's dialogs use it, and a component
     writes through `Context.GetAllowedAddress`, logging a refusal by its scheme.
   - Every link and source a component writes from data brought to it, each with a hostile test.
   - The composer's conversion, history and template paths now sanitize, and pasted markup keeps an address only
     where its element's kind allows it, which a control character or a tab inside a `javascript:` address slipped
     past before.
   - `SinkInventoryTests` reads the source for every markup and address sink and fails on one no hostile test covers
     or no reason excuses.
3. ✅ **Malformed input refused, never thrown.** Every method a script calls back into .NET, and every input that parses
   text, given a NUL character, a lone surrogate, text where a number belongs and a value far past any length, each
   ending neither a circuit nor an application.
   - Each of the 33 methods a script calls back into .NET refuses what it can't use, logged at `Debug`, each tested as
     the browser would call it. Four could end a circuit before: an infinite number reaching a frame's placement, a
     grid's column width or a freeze pane's state; a year past the calendar's range; the color picker's alpha from
     text that isn't hex; and an upload the browser describes with a negative size.
   - Every input parsing typed text refuses a NUL, a lone surrogate, a non-number and a megabyte as a validation
     message, each tested.
   - A token or a key that names nothing stays silent where it's a normal race, a data view's row redrawn or an
     explorer's drop.
   - What the first pass left:
     - ✅ What a script answers to .NET's own calls, an observation's first state among them, which the framework read
       as a typed value; a freeze pane's first state with an infinite extent ended a circuit.
       - `JSModule`, `JSInterop` and the DOM watcher's and engine's session ask the framework for JSON alone and read it
         through the caller's contract, refusing at `Debug` an answer that isn't the value asked for. A malformed
         answer to a call of the watcher or the engine logged a `Warning` before, as a call that failed.
       - The document order, the element labels and the composer's link dialog, which read their JSON by hand and
         threw on what they couldn't, read it the same way; an order that doesn't place each element once is refused.
       - `JSTransfer.Deserialize` reads numbers strictly whatever a contract allows. The web's defaults read the text
         `"Infinity"` as a number, which let an infinite extent through a freeze pane's later states too.
     - ✅ A culture name outside the application's cultures is to be refused at `Debug` before the culture provider is
       asked, and the browser's token taken out of the provider's `Error` message. The application's cultures are
       those the platform defines, whose names every culture an application uses keeps, so a name the platform would
       make up, `qq-ZZ` among them, is refused before the provider is asked, and with no provider the platform no
       longer makes a culture up for it. No message the culture coordination or culture routing logs above `Debug`
       names a culture, a route's token or an address the browser sent.
     - ✅ Input refusals are to log under the component that owns the registration. `InputRegistration` takes its
       owner's `IJSEnvironment`, which every component hands it from its context, and logs a refused action or
       dismissal under the component's category and identity; the host's routes, which the router owns, log under it.
4. ✅ **Limits.** `FileUploaderOptions.MaxFileSize` set to 10 MB by default, with the changelog saying how a host raises
   it; every other size Forma accepts from the browser found, capped and checked; and each package's README naming the
   framework limits a host sets.
   - The uploader reads up to its cap, 10 MiB by default, refusing a larger file before reading it and stopping a read
     that runs past it; the explorer's and the composer's uploads take the same cap.
   - A text field's `MaxLength` holds in .NET, and an edit waiting for its predecessors waits at most 256 revisions.
   - A virtualized list draws at most a screen's worth whatever viewport the browser reports, and a data view reads a
     selection's tokens no further than the items it holds tokens for.
   - The READMEs name SignalR's `MaximumReceiveMessageSize`, which Forma needs no host to raise, beside Forma's own
     limits.
   - The READMEs name the one message Blazor's `InputFile` describes a whole selection in, which a very large one can
     push past the hub's limit, and the Server's README a Host header no address can be built from, which a host
     refuses itself.
5. ✅ **Dependencies.** The full gate's job checking every NuGet and npm package for known vulnerabilities.
   - `scripts/Test-Dependencies.ps1` lists the solution's packages with `dotnet list package --vulnerable
     --include-transitive` and audits the script tests' with `npm audit`; the task scripts' contract check runs it on
     reports made for it. The first run found no vulnerable package.
   - A failure leaves the job's marker for the nightly report, which files it as an issue.
6. ✅ **A scan of the running Showcase.** `scripts/Invoke-SecurityScan.ps1` and OWASP ZAP in a container, a first scan,
   and each finding and logged error fixed or explained in the design.
   - The script runs ZAP 2.17.0's automation plan, `scripts/zap/ScanShowcase.yaml`: the plain spider, which reaches all
     74 of the Showcase's pages in its four cultures, since each is rendered on the server first; the malformed requests
     the design names; the passive scan; the active scan of every query, form field, path segment, header and cookie;
     and the DOM XSS rule on each address's query and fragment, on eight threads. ZAP's recorder notes each server
     error as it comes. The task scripts' contract check runs the script on runs made for it.
   - The first scan found two defects, both the Showcase's, and none of the library's. The Stepper page answered any
     `step` in its query that wasn't a number with a 500, since it bound the query to an `int?`; it reads the step as
     text now and takes only a number. A Host header no address can be built from, which Kestrel lets through, made
     the framework throw as it built the page's address; the Showcase refuses such a request with a 400 ahead of its
     pipeline now. The Showcase's first suites, a bUnit one and a unit one, hold the two. The first scan's 38 SQL
     injection alerts, against a Showcase with no database, didn't recur once the Stepper page stopped answering with a
     500.
   - The first scan stopped at the DOM XSS rule, which, run with the other rules on every input, drove a browser on
     each of 32 threads past 12 GB; it runs in a pass of its own on the address alone since. The second scan ran for
     three and a half hours, two and a quarter of them that pass, whose browsers started a process for each site the
     video player's embedded players load; the pass has left that page out since.
   - The second scan answered no request with a 500. Every alert and logged error it raised is explained in the design:
     the Content Security Policy, which is step 7's, the headers and the HTTPS a host's server gives, the culture cookie
     the page's script reads, a number in a sample video, and the antiforgery service, which logs a cookie it can't read
     at Error.
7. ✅ **A Content Security Policy.** The check pages served under a strict policy, what it blocks fixed, and the README's
   policy for a host.
   - The Server check host sends the policy as a header with a nonce of each request's own, which `FormaPageHead`'s new
     `Nonce` gives the import map; the web view hosts write it as a meta element. A check fails on any violation its
     pages report.
   - Every browser check passes under it. What it found: a plugin's module at a relative address was read against
     Forma's own script and not found, now read against the page's base; and, the checks' own, plugin assets declared as
     `data:` addresses, downloads read back with `fetch(blob)`, and Blazor's default reconnection display, whose
     inline style element a host replaces with a display of its own, as the README now says.

## Open

- ✅ Step 3's answers.
- ✅ Step 3's culture names.
- ✅ Step 3's input logs.
- ✅ Step 7.
- ✅ Inline styles written from data join step 2's sinks: the inventory finds each, and each is a typed CSS value or a
  sink with a hostile test.
  - `SinkInventoryTests` finds every style attribute bound to data, every style a render tree writes, the declarations
    a style builder adds as text and every style a script sets, 54 in all, and holds each to a hostile test, a typed
    value or a reason no data reaches it.
  - `CssStyleBuilder` takes a declaration's value as a `CssLength` or a `CssColorValue` alone, with the host's own text
    added through `AddDeclarations`, and a toolbar option's swatch is a `CssColorValue`, where the toolbar wrote the
    string it was given into its style.
  - The typography plugin previews a font family only where it is a list of family names, so a hostile one can't
    write a declaration of its own into the toolbar, and the color picker's swatch, the sanitizer's kept style and the
    preview each have a hostile test.
