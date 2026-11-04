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
- **No .NET handler takes a text field's events.** Blazor sends the value of an event's target with every event a
  .NET handler takes, wherever the handler is, so no Forma component takes in .NET an event a text field raises:
  presses and keys reach it through the input router, or through its own script where it only observes a press
  its content owns, and focus and a field's selection through the DOM watcher.
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

Blazor sends the value of an event's target with every event a .NET handler takes, whichever element the handler
is on, so a handler on a text field, or on any element an event raised in a text field reaches, carries the
field's whole text. No Forma component takes such an event in .NET: its text crosses through the field's edits,
its presses and keys through the input router, a press a component only observes, as a window brought forward by
any press inside it, through that component's own script, and its focus and selection through the DOM watcher's
observations. A host's own handler is the host's to keep off its fields.

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
edit never parts a surrogate pair, which its inserted text couldn't carry. An edit .NET can't apply, its
inserted text refused or broken on its way or its range outside the copy, is a disagreement, which .NET resolves
by asking the field for its whole text, which arrives through the transfer as a **replacement** in the stream of
the field's edits, standing at the field's revision; the edits it supersedes are dropped, and a commit among
them is carried by it. As the connection comes back, .NET asks each field for its whole text where its copy
stands at another revision, so an edit lost while the connection was down is replaced too.

Revisions number a field's edits across every registration of it. A field whose element is drawn anew, or whose
options change, goes on from the revision and the text its previous registration left, and an element drawn in
place of another takes the text the reader left there. A field the reader typed into before its edits attached
keeps what it holds, and sends what it holds beyond the copy as its first edit.

A field updating as each character is typed sends an edit on each input; a field updating on change sends the
edits since its last commit as it commits, so in either mode a message's size follows the edit, not the text.

Edits are sent in the order they happen, each as it happens: never held back until .NET acknowledges the one
before it, which would let an action overtake an edit still waiting, and never coalesced to the latest, since
each revision builds on the one before. .NET applies them in revision order: an edit arriving ahead of its
predecessor, as one streaming past the budget can be overtaken by a smaller one after it, waits for it, and an
edit whose revision the copy has already passed is dropped, so an edit completing late never puts older text
back. Each change an applied edit makes is handed to the field's component in turn, the next applied once the
component has finished with the last, so a commit's work is done before the edit after it; a component's own
pause, such as a search's or a commit's debounce, runs apart from the change it follows.

The character count follows every keystroke in the browser, whichever mode the field updates in, as part of the
field's edits, so a text .NET writes is counted as it lands. An editor whose content is a document sends the
edits of its serialized content in the same way, at its own pace: its script provides the text its edits read
and write, the document as it serializes it, and commits it as the reader pauses, as the editor loses the focus,
as the input router commits it before an action, and as a command it runs leaves a document, ahead of the
command's answer. A command answers with what it alone carries, as the selection it leaves, never the document.

While a reader edits a field, its text belongs to the browser: .NET doesn't render it back, so typing that
outpaces a render is never reverted. The field renders the text it was last rendered or written with for as long
as the text its component shows is the copy's. A change .NET makes of its own, a host setting the bound value or
a field writing a number it read in its own format, is the text the component shows departing from the copy: the
field renders it and writes it through its script, which answers the revision the field then stands at; the edits
arriving meanwhile wait, and those the write supersedes are dropped. A write the field hasn't taken is made once
the field is attached again, or as the connection comes back. Rendered statically, a field renders its value and
edits nothing.

## The edit boundary

When a user invokes a participating action, validation and the action handler see every edit made in that form
before the action. If Forma cannot apply an earlier edit, it reports the failure and stops the action before it
saves stale data.

- **The boundary is the browser's own order.** The browser sends a field's edits, and the commit a field makes
  as the reader leaves it or as the input router routes an action from it, ahead of the action's own message, so
  every edit made before the action has reached .NET as the action starts. The action waits until each field in
  its form holds every edit received: applied, a handler's work on it included, not merely received, since a
  handler that awaits before it applies lets the action run in between. A field whose edits were still attaching
  as the action began commits what it holds once they land, since what the reader typed before they listened
  went unsent, and a pause a component keeps of its own, as a commit's debounce, is cut short by its action and
  by its form's.
  Later typing, or an upload in another form, never keeps the action waiting.
- **The wait comes before validation.** A form's submit path waits before it validates, so clicking Save and
  pressing Enter behave the same; a wait inside the handler a valid submit calls would come after validation had
  read the old value. Edit delivery and its acknowledgement keep running while the action waits.
- **A failure stops the action.** An edit that can't be applied, a transfer refused or broken among them, is
  reported on its field, and the action it held back never runs.

Forma's buttons and input scopes inside a form wait for the form's fields, a component acting on its own field
waits for that field, and a form's submit through Forma's helper waits for the form's. A composer in a form is
one of its fields: the editor in its surface settles its edits, cuts its pause short and reports its selection,
as it does before any command the composer runs, before the composer converts the document for another
editor, and before a view takes the editor's place, since the editor's edits go with it. A host's form opts in
through that helper, which takes the form's submit path in place of its own validation and handler call, so a
host keeps its own components and gains the guarantee without deriving them from a Forma base. A host's own
control outside a participating action reads the bound model as it stands.
