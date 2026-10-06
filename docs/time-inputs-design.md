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
- **Valid times and suggested times are separate.** `Min` and `Max` bound the valid times,
  and an optional `Step` restricts them to whole steps counted from `Min`, or from midnight
  where there is no `Min`; a time off the step is reported, never rounded onto it. The list
  offers times at its own interval from the range's start, half an hour by default or each
  step where there is a step, and offering a time never makes another one invalid: against
  a list of half hours, a typed quarter hour is valid unless `Step` says otherwise. Every
  time the list offers is one the field takes, so an interval that is not a whole number of
  steps is refused.
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
  interval of seconds over a whole day draws only the options near the viewport.
- **Two values or one.** A date and a time an application keeps apart are edited with the
  date picker and the time picker, each standing on its own. One value holding both is
  edited with the date and time picker, which sets the date picker's control beside the
  time picker's under one field: one label, one required state, and the messages of the
  one value they edit together. Each picker's control therefore stands apart from the
  field that frames it, so the standalone picker frames it in a field of its own and the
  combined picker frames both in one, with no field inside another and no label twice.
