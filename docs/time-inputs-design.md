# Forma Time Inputs Design

## Scope

The inputs that edit a time of day: `FormaTimePicker`, a time alone, and
`FormaDateTimePicker`, one value holding a date and its time, which composes the date
picker's control and the time picker's.

## Purpose

A time is typed the way the formatting culture writes it or chosen from a list of
suggested times, and a time the field cannot take is reported, never rounded or written.
The rules for what a valid time is and for what the list offers are separate, so a list of
half hours never makes a quarter hour invalid.

## Decision summary

- **What binds.** The time picker binds a `TimeOnly` or a nullable one, and the date and
  time picker a `DateTime` or a `DateTimeOffset`, or a nullable one. A value that cannot be
  empty is required, and a nullable one is required where the host sets `Required`; the
  label's mark and the control's required state follow that one effective state, as the
  date picker's and the number input's do.
- **The formatting culture writes and reads it.** A time is written and read in
  `CultureInfo.CurrentCulture`, the culture that formats, which can differ from the one the
  field's own strings come from: 24-hour or 12-hour with the culture's designators, as the
  culture writes it. Display carries no more than the value needs.
- **No time zone.** A time of day has none. A date with a time is edited on its own clock: a
  `DateTime` keeps its kind and a `DateTimeOffset` its offset, and a host showing the
  reader's zone converts before binding.
- **A new value's kind and offset.** A date with a time entered where the value was empty is
  a `DateTime` of unspecified kind, or a `DateTimeOffset` at the offset the host gives, zero
  where it gives none. The zero offset is how the entry is read, never a conversion: 14:00
  entered is 14:00 +00:00, never the reader's 14:00 turned into UTC. An offset is not a time
  zone, so keeping +03:00 across a change of date follows no daylight saving; a host that
  needs a zone's rules resolves the offset for the date before binding.
- **Valid times and suggested times are separate.** `Min` and `Max` bound the valid times,
  and an optional `Step` restricts them to whole steps counted from `Min`, or from midnight
  where there is no `Min`; a time off the step is reported, never rounded onto it. The list
  offers times at its own interval from the range's start, half an hour by default or each
  step where there is a step, and offering a time never makes another one invalid: against
  a list of half hours, a typed quarter hour is valid unless `Step` says otherwise. Every
  time the list offers is one the field takes, so an interval that is not a whole number of
  steps is refused.
- **A date with a time is bounded as one value.** The date and time picker's `Min` and `Max`
  are values of the type it binds and bound the date with its time, inclusive at both ends:
  between 7 October at 14:00 and 9 October at 11:00, 7 October takes 14:00 and later, 8
  October any time, and 9 October 11:00 and earlier. A date outside the bounds cannot be
  picked, a typed time follows the same bounds, and a date changed under a time the new date
  does not allow reports that time, never moves it. `Step` and `ListInterval` set the
  time's steps and the list's interval as the time picker's do, counted from `Min`'s time of
  day, or midnight where there is no `Min`, and never redraw the bounds; the list on a given
  date runs through the times that date allows. A window of hours that holds every day, 09:00
  to 17:00, is a separate rule, not part of these bounds.
- **Half a date with a time.** The date and the time are entered apart and written
  together. With both empty, the value is empty where it can be, and the field reports it
  required where it cannot. With one entered and the other not, what was entered stays as
  it is, nothing is written, and the missing half is reported once the focus leaves the
  field or the form submits, never while the reader is between the two. Clearing one half
  of a value keeps the edit on show, reported as incomplete; the value bound before stays
  bound, and the report blocks a valid submission. With both entered, they are written
  together as one value, which the bounds then judge.
- **A range can run across midnight.** `Min` later than `Max` is a range through midnight,
  22:00 to 02:00, inclusive at both ends, and the validation, the step's origin and the list
  all follow it, the list running from `Min` round to `Max`.
- **Precision is kept.** Seconds are shown wherever the value, a limit, the step or the
  list's interval has them, so a value of 09:30:45 never reads as 09:30. Text the reader
  has not edited is never written back, so a value carries its sub-second ticks until the
  reader changes it.
- **The field is an editable combobox.** The text field holds the focus and the list opens
  in a popover beneath it, its options named by the field's `aria-activedescendant`. Down
  opens the list and moves forward through it, Up moves back, Enter takes the highlighted
  time, and Escape closes the list without taking it; the arrows move through the list and
  never step the value itself. The list follows no typing; it opens on the suggestion
  nearest the time the field holds.
- **Committing.** Typed text is written when the field loses the focus and before the form
  submits, and text it cannot take is reported, stays in the field for correcting and
  blocks a valid submission. Choosing a suggestion writes it at once and closes the list.
- **A long list draws only what shows.** The list is drawn through `FormaVirtualize`, so an
  interval of seconds over a whole day draws only the options near the viewport, each one
  still giving its place among every suggestion the list holds.
- **Two values or one.** A date and a time an application keeps apart are edited with the
  date picker and the time picker, each standing on its own. One value holding both is
  edited with the date and time picker, which sets the date picker's control beside the
  time picker's under one field: one label, one required state, and the messages of the
  one value they edit together. Each picker's control therefore stands apart from the
  field that frames it, so the standalone picker frames it in a field of its own and the
  combined picker frames both in one, with no field inside another and no label twice.
- **Where the label sits.** The time picker's label is pinned above its value, as the date
  picker's is, since the field's floating label reads its state from a text element the
  time picker's control wraps. The date and time picker's label stands above its two
  controls, which sit side by side at an input's own height and wrap one beneath the other
  where the width runs out, since a label floating inside one box would name only that box.
- **Read-only.** A read-only time is a read-only text field whose list cannot be opened. A
  read-only date is shown as read-only text named as its trigger is, with no calendar to
  open and nothing to clear, since a button has no read-only state to announce.
- **One label over two controls.** A field either labels the one control it frames, its
  label pointing at that control, or names a group of the controls it frames, the group
  taking its label as its name. The date and time picker's field names a group: the group
  is the field's label, "Starts", and each control inside it carries a name of its own that
  reads on its own, the date "Date" and the time "Time".
