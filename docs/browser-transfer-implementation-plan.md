# Forma Browser Transfer Implementation Plan

The plan for building what [the browser transfer design](browser-transfer-design.md) states, and the record of
what has landed. Every component and operation the survey below found takes part; none is left to a later pass.

## The survey

Every way a value a reader can make large crosses from the browser to .NET, at `9e9ed874`.

| Channel | Where | What crosses |
| --- | --- | --- |
| Blazor's input and change events | `FormaTextBox` and what draws it (`FormaPagination`'s entry, the explorer's rename and new folder entries, the composer's link, image, video, embed and table dialogs), `FormaTextArea`, `FormaPasswordBox`, `FormaSearchBox` and what draws it (`FormaSearchToolbar`, `FormaMultiSelect`'s filter, `FormaEmojiPicker`'s search, the explorer's search box), `FormaNumberInput` and `FormaCurrencyInput`, which draws it, `FormaTimeBox` and the time and date-and-time pickers that draw it, `FormaPlainTextEditor` | The field's whole value, twice per message, JSON-escaped |
| Blazor's other event handlers an event raised in a text field reaches | `FormaWindow`'s press and focus, `FormaDataTree`'s node focus, `FormaPagination`'s entry focus, the explorer search's focus, `FormaTimeBox`'s focus leaving, `FormaPlainTextEditor`'s click and key-up | The field's whole value with each event |
| A script calling .NET | `FormaRichTextEditor`'s content | The document's HTML at every pause in typing |
| A script calling .NET | The composer's image, video and embed activations | What the activated element holds |
| A script calling .NET | `FormaDataView`'s range selection | Every item token the range spans |
| A script calling .NET | The DOM watcher's receiver | A capability's state |
| A script answering .NET | `TextEntry.take`, which `FormaPagination` and `FormaPlainTextEditor` call | A field's whole text |
| A script answering .NET | `Print.compose` | The composed document |
| A script answering .NET | `DocumentOrder.sort`, which `FormaDropDownMenu` calls | A permutation of every item it is given |

File uploads already stream, the calls that carry scalars (a wheel's direction, a resize's travel, a frame's
placement, a culture) stay small whatever the reader does, and the server-to-browser direction isn't capped.

The DOM watcher and engine, and the conversion of Forma's scripts to DOMQL, keep the transport to C# as Forma's
own host layer, so the transfer belongs to that layer and every converted script keeps it.

## Steps

1. **The transfer.** ✅ The browser half in the JavaScript interop floor, measuring a call's encoded size against
   the budget and streaming a value past it, and the .NET half beside `JSModule`, reading an inline value or a
   stream up to the size its receiver accepts and reporting a refusal or a broken stream; the registrar's
   configured maximum and its default; the DOM watcher's receiver and every call answering .NET with content
   taking it. Gate: script and unit tests of the budget measured with escaping and the call's other arguments, a
   stream at the accepted size and past it, a broken and a stalled stream, and inline values unchanged. Every call
   answering .NET with content took the transfer here: `TextEntry.take`, `Print.compose` and its page's
   composition, and `DocumentOrder.sort`. A host sets the accepted maximum by registering its own
   `JSTransferOptions`, 16 MiB by default. A stream's reference is released once its stream has been read, since
   WebAssembly pulls each chunk through it, and a stream no receiver takes is released unread.
2. **Edits.** ✅ `FormaTextBox`, `FormaTextArea`, `FormaPasswordBox`, `FormaSearchBox`, `FormaNumberInput`,
   `FormaTimeBox`, `FormaPlainTextEditor` and everything that draws them send edits through their scripts in place
   of Blazor's binding, in both update modes, with the browser holding the text it edits, .NET's own changes
   written through the script, and static rendering unchanged; and the components whose Blazor handlers an event
   raised in a text field reaches take those events through the input router and the DOM watcher. Gate: unit and
   script tests of applying edits, a disagreement resolved through the transfer, and a host's change written back;
   bUnit tests of both modes, validation following each applied edit or commit, and the count; and no Forma
   component left with a .NET handler a text field's event reaches. The fields' edits landed first, with step 4.
   The DOM watcher's focus observation gained the item holding the focus, named by an attribute the surface's
   items carry, which the data tree follows its nodes by, and the watcher gained a text field's selection, which
   the plain text editor reports to the composer once the reader pauses, as its key-up did, and reads afresh for a
   command, in both update modes. The window, the pagination's entry, the explorer's search and the time box
   follow the focus through the watcher. Departures: the running count is an option of the field's edits rather
   than a capability of its own, so a text .NET writes is counted as it lands, and the standalone count retired
   with no other user; the bUnit tests type through a stand-in for the field's script, since a field takes no
   Blazor event to trigger; a window hears of a press inside it from its own script, which already listened for
   presses to hold a raise until they end, since a window only observes a press its content owns; `TextEntry.take`
   retired with its one caller, since the pagination's entry holds what the reader typed before the circuit
   connected once its edits settle; and a focus observation sends every state as it is measured, rather than one
   in flight with the latest waiting, since the data tree's browser check found a click on a node's disclosure
   overtaking the focus held behind an earlier one, as the DOM watcher's design records of occurrences, and the
   tree's keys and disclosures wait for its focus watch, whose first state says where the focus is, as a Blazor
   handler live from the first render had known.
3. **The editor and the channels beside it.** `FormaRichTextEditor`'s content as edits of its serialized HTML at
   its pace; the composer's activations through the transfer; and the data view's range selection through the
   transfer ✅, a range lost on its way logged under the data view and left unselected. Gate: unit and script tests
   of each, at a size past the budget.
4. **The edit boundary.** ✅ The browser's capture of the latest revision in the action's form or interaction
   scope, flushing debounced edits through it; Forma's buttons, the input router's actions and the form helper
   waiting for it before validation, with delivery running meanwhile; a failure reported on its field and the
   action stopped; the helper a host's form opts into. Gate: unit and script tests of a boundary that edits in
   another form don't hold, and of a failed edit stopping its action. Landed with step 2's fields, whose browser
   checks it was needed for: a field's edits attach a round trip after it renders, and an action raised in that
   window read its field before the field listened. Departure: the boundary is the order the browser sends in
   rather than a revision it captures, since a field's edits and commits already reach .NET ahead of the action,
   and a field whose edits were attaching commits what it holds once they land, which no capture in the browser
   could have held. The form's fields share a scope kept on its `EditContext`; `FormaButton` and `FormaInputScope`
   settle it, the pagination its entry and the plain text editor its own text, and `SubmitAsync` and
   `SettleEditsAsync` are the helper.
5. **The checks.** Browser checks on Interactive Server with Blazor's default 32 KB hub limit, a multi-megabyte
   value through every channel above, and the boundary's races: the final word typed and Save clicked inside
   the editor's debounce window; a multi-megabyte value pasted, its transfer held open, and the form submitted
   with Enter; each through a Forma form and an opted-in host form, asserting the exact value validation and
   Save see, and that a failed transfer never invokes Save; the same ordering checks on WebAssembly and in the
   web view. The README tells a host the transfer keeps every Forma value under the default limit. Gate: the
   full gate and these checks, which are what Sterling Bourse's removal of its raised limit waits on.
