# Forma Time Inputs Implementation Plan

The plan for building the inputs [the time inputs design](time-inputs-design.md) states:
the steps, their state, and where the build departed from the design. Section names refer
to the design.

## Steps

1. **The time picker.** Open. `FormaTimePicker` with every rule of the design's decision
   summary: the binding and its required state, reading and writing in the formatting
   culture, the range with its step and its overnight form, the precision kept, and the
   editable combobox with its list drawn through the virtualizer, its control standing
   apart from the field that frames it, as the design's two values or one asks. Its
   Showcase page shows every state, and the data view's property column edits a time with
   it. Gate: tests of each rule, a browser pass in English, Hebrew and Spanish under a fine
   pointer and a coarse one, the combobox's keys and names checked in Chrome, and the
   screen reader check gaining the time picker's scenario. Landed, in review: the picker
   and its box, its page, and the property column editing a time with it. Departure: the
   field's label is pinned above it, as the date picker's is, since the box wraps the text
   element the field's floating label reads its state from. The screen reader scenario's
   first run found the list's suggestions counted among those drawn, "1 of 7" in a day of
   48, since the list draws only what it shows; each suggestion now carries its place among
   all of them, which the scenario hears as "1 of 48".
2. **The date and time picker.** Open. The date picker's control set apart from its field,
   as the time picker's is, then `FormaDateTimePicker` setting the two controls side by side
   under one field with one label, one required state and one value's messages, and the
   property column editing a date with a time with it. Gate: as the time picker's, with a
   `DateTime` keeping its kind and a `DateTimeOffset` its offset, and no field nested in
   another nor any label twice.
