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
3. **Malformed input refused, never thrown.** Every method a script calls back into .NET, and every input that parses
   text, given a NUL character, a lone surrogate, text where a number belongs and a value far past any length, each
   ending neither a circuit nor an application.
4. **Limits.** `FileUploaderOptions.MaxFileSize` set to 10 MB by default, with the changelog saying how a host raises
   it; every other size Forma accepts from the browser found, capped and checked; and each package's README naming the
   framework limits a host sets.
5. ✅ **Dependencies.** The full gate's job checking every NuGet and npm package for known vulnerabilities.
   - `scripts/Test-Dependencies.ps1` lists the solution's packages with `dotnet list package --vulnerable
     --include-transitive` and audits the script tests' with `npm audit`; the task scripts' contract check runs it on
     reports made for it. The first run found no vulnerable package.
   - A failure leaves the job's marker for the nightly report, which files it as an issue.
6. **A scan of the running Showcase.** `scripts/Invoke-SecurityScan.ps1` and OWASP ZAP in a container, a first scan,
   and each finding and logged error fixed or explained in the design.
7. **A Content Security Policy.** The check pages served under a strict policy, what it blocks fixed, and the README's
   policy for a host.

## Open

- Steps 3, 4, 6 and 7.
