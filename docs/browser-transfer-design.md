# Forma Browser Transfer Design

## Scope

How a value travels from the browser to .NET. Authoritative for the transfer in the JavaScript interop
floor, every component and operation whose browser half sends .NET a value a reader can make large, the
edits a text field sends as its text changes, and the edit boundary a participating action waits for.

## Purpose

Under Interactive Server, every value the browser sends .NET travels in a circuit message, which the hub's
`MaximumReceiveMessageSize` caps, 32 KB by default. A value past that cap closes the circuit, and the reader's
unsaved work goes with it. Blazor's own event payloads carry a field's whole value, twice, JSON-escaped, on
every input event; a script's call into .NET, or its answer to one, carries whatever it is handed. A text a
reader types or pastes, a document an editor holds, the items a selection spans and a page composed for
printing all grow without a bound the transport respects, so each is a failure waiting for a large enough
value, whichever component carries it.

Forma answers this once, in the floor every script builds on, rather than per component: no value a reader can
make large travels in a single message, a text typed live sends what changed rather than all of it, and an
action that reads a field's text waits until the text it reads is the one the reader saw.

## Decision summary

- **One transfer.** Every value a reader can make large crosses from the browser to .NET through the transfer:
  inline while the call that carries it fits a fixed budget, and as a JavaScript-to-.NET stream beyond it,
  which Blazor sends in chunks under any message limit. The receiver names the size it accepts.
- **Edits, not values.** A text field sends .NET the edits its text undergoes, each a revision with what it
  removed and what it inserted, and .NET applies them to its copy; the whole text crosses only through the
  transfer, where the two copies disagree.
- **The browser holds the text it edits.** While a reader edits a field, its text is the browser's; .NET writes
  it, through the field's script, only for a change of its own.
- **The edit boundary.** When a user invokes a participating action, validation and the action handler see
  every edit made in that form before the action. If Forma cannot apply an earlier edit, it reports the failure
  and stops the action before it saves stale data.
- **Participation.** Forma's buttons, its input router's actions and a form's submit participate; a host's form
  opts in through a helper, never a required base, so an ordinary Blazor form keeps its own components.

## The transfer

The transfer is the one way a value a reader can make large crosses from the browser to .NET, in all three of
the ways a value crosses: a script calling .NET, a script answering a call .NET made, and the DOM watcher's
receiver delivering a state. Blazor's own handlers that carry a whole value, `@bind` on a text field's input or
change event among them, are not used on these paths, since a message they send beside the transfer would still
pass the cap.

A value travels inline while the call that carries it fits the **budget**, a fixed size under Blazor's default
hub limit, measured on the call as it is encoded: its JSON, escaping included, with the call's other arguments
and the message's framing. The budget doesn't follow a host's configured limit, so a host on Blazor's default
is served as one that raised it. A value past the budget travels as a JavaScript-to-.NET stream of its UTF-8
bytes, which Blazor sends in chunks sized under the hub limit, and the call carries the stream in its place.

The receiver names the size it **accepts**, explicitly, wherever a value can stream: a field accepts what its
maximum length allows, and a receiver with no maximum of its own accepts the maximum the host configures where
it registers Forma, with a default the registrar documents. A stream's own default limit never decides it. A
value larger than the receiver accepts is refused, and a stream that breaks, stalls or ends short is a failure;
either is reported to the receiver, which applies nothing of it.

Under WebAssembly and in a web view no hub stands between the browser and .NET, and the transfer works there as
it does on the Server, so a value is carried one way in every runtime and keeps its order with the calls
around it.

## Edits

A text field sends .NET what its text undergoes, never its whole text on an event. Each **edit** carries the
field's **revision**, the position it starts at, the length it removed and the text it inserted, computed
against the text the field last sent, and .NET applies it to its copy, which then stands at that revision. An
edit whose revision doesn't follow the copy's is a disagreement, which .NET resolves by asking for the whole
text through the transfer.

A field updating as each character is typed sends an edit on each input; a field updating on change sends the
edits since its last commit as it commits, so in either mode a message's size follows the edit, not the text.

Edits are sent in the order they happen, each as it happens: never held back until .NET acknowledges the one
before it, which would let an action overtake an edit still waiting, and never coalesced to the latest, since
each revision builds on the one before. .NET applies them in revision order: an edit arriving ahead of its
predecessor, as one streaming past the budget can be overtaken by a smaller one after it, waits for it, and an
edit whose revision the copy has already passed is dropped, so an edit completing late never puts older text
back.
The character count follows every keystroke in the browser, whichever mode the field updates in. An editor
whose content is a document sends the edits of its serialized content in the same way, at its own pace.

While a reader edits a field, its text belongs to the browser: .NET doesn't render it back, so typing that
outpaces a render is never reverted. A change .NET makes of its own, a host setting the bound value, reaches the
field through its script, which writes it and moves the revision on. Rendered statically, a field renders its
value and edits nothing.

## The edit boundary

When a user invokes a participating action, validation and the action handler see every edit made in that form
before the action. If Forma cannot apply an earlier edit, it reports the failure and stops the action before it
saves stale data.

- **The boundary is captured in the browser.** At the click or the submit, the browser identifies the latest
  edit revision in the relevant form or interaction scope, flushes any debounced edits through that revision,
  and the action waits until those edits are reflected in the bound model: applied, not merely received, since a
  handler that awaits before it applies lets the action run in between. Later typing, or an upload in another
  form, never keeps the action waiting.
- **The wait comes before validation.** A form's submit path waits before it validates, so clicking Save and
  pressing Enter behave the same; a wait inside the handler a valid submit calls would come after validation had
  read the old value. Edit delivery and its acknowledgement keep running while the action waits.
- **A failure stops the action.** An edit that can't be applied, a transfer refused or broken among them, is
  reported on its field, and the action it held back never runs.

Forma's buttons, the actions its input router routes and a form's submit through Forma's helper participate.
A host's form opts in through that helper, which takes the form's submit path in place of its own validation
and handler call, so a host keeps its own components and gains the guarantee without deriving them from a
Forma base. A host's own control outside a participating action reads the bound model as it stands.
