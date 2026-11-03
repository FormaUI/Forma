# Forma Browser Transfer Implementation Plan

The plan for building what [the browser transfer design](browser-transfer-design.md) states, and the record of
what has landed. Every component and operation the survey below found takes part; none is left to a later pass.

## The survey

Every way a value a reader can make large crosses from the browser to .NET, at `9e9ed874`.

| Channel | Where | What crosses |
| --- | --- | --- |
| Blazor's input and change events | `FormaTextBox` and what draws it (`FormaPasswordBox`, `FormaSearchBox`, `FormaNumberInput`, `FormaTimeBox`, `FormaPagination`'s entry, the composer's link, image, video, embed and table dialogs), `FormaTextArea`, `FormaPlainTextEditor` | The field's whole value, twice per message, JSON-escaped |
| A script calling .NET | `FormaRichTextEditor`'s content | The document's HTML at every pause in typing |
| A script calling .NET | The composer's image, video and embed activations | What the activated element holds |
| A script calling .NET | `FormaDataView`'s range selection | Every item token the range spans |
| A script calling .NET | The DOM watcher's receiver | A capability's state |
| A script answering .NET | `TextEntry.take`, which `FormaPagination` and `FormaPlainTextEditor` call | A field's whole text |
| A script answering .NET | `Print.compose` | The composed document |
| A script answering .NET | The data view's sort | A permutation of every item |

File uploads already stream, the calls that carry scalars (a wheel's direction, a resize's travel, a frame's
placement, a culture) stay small whatever the reader does, and the server-to-browser direction isn't capped.

The DOM watcher and engine, and the conversion of Forma's scripts to DOMQL, keep the transport to C# as Forma's
own host layer, so the transfer belongs to that layer and every converted script keeps it.

## Steps

1. **The transfer.** The browser half in the JavaScript interop floor, measuring a call's encoded size against
   the budget and streaming a value past it, and the .NET half beside `JSModule`, reading an inline value or a
   stream up to the size its receiver accepts and reporting a refusal or a broken stream; the registrar's
   configured maximum and its default; the DOM watcher's receiver and every call answering .NET with content
   taking it. Gate: script and unit tests of the budget measured with escaping and the call's other arguments,
   a stream at the accepted size and past it, a broken and a stalled stream, and inline values unchanged.
2. **Edits.** `FormaTextBox` and everything that draws it, `FormaTextArea` and `FormaPlainTextEditor` send
   edits through their scripts in place of Blazor's binding, in both update modes, with the browser holding the
   text it edits, .NET's own changes written through the script, and static rendering unchanged. Gate: unit and
   script tests of applying edits, a disagreement resolved through the transfer, and a host's change written
   back; bUnit tests of both modes, validation following each applied edit or commit, and the count.
3. **The editor and the channels beside it.** `FormaRichTextEditor`'s content as edits of its serialized HTML at
   its pace; the composer's activations, the data view's range selection and sort, `TextEntry.take` and
   `Print.compose` through the transfer. Gate: unit and script tests of each, at a size past the budget.
4. **The edit boundary.** The browser's capture of the latest revision in the action's form or interaction
   scope, flushing debounced edits through it; Forma's buttons, the input router's actions and the form helper
   waiting for it before validation, with delivery running meanwhile; a failure reported on its field and the
   action stopped; the helper a host's form opts into. Gate: unit and script tests of a boundary that edits in
   another form don't hold, and of a failed edit stopping its action.
5. **The checks.** Browser checks on Interactive Server with Blazor's default 32 KB hub limit, a multi-megabyte
   value through every channel above, and the boundary's races: the final word typed and Save clicked inside
   the editor's debounce window; a multi-megabyte value pasted, its transfer held open, and the form submitted
   with Enter; each through a Forma form and an opted-in host form, asserting the exact value validation and
   Save see, and that a failed transfer never invokes Save; the same ordering checks on WebAssembly and in the
   web view. The README tells a host the transfer keeps every Forma value under the default limit. Gate: the
   full gate and these checks, which are what Sterling Bourse's removal of its raised limit waits on.
