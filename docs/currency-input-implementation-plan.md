# Forma Currency Input Implementation Plan

The plan for building what [the currency input design](currency-input-design.md) states: the
steps, their state, and where the build departed from the design. Section names refer to the
design.

## Steps

1. **The currency catalog.** Open. `Forma.Framework.Currencies` with `CurrencyCatalog`, its
   snapshot generated from CLDR by an update script beside the plural rules', and its tests: a
   currency's minor units, its symbol in a culture through locale inheritance, and the ISO code
   where no locale has one. Gate: the snapshot's size measured with every locale and with CLDR's
   modern ones before deciding which locales the first release ships, and the compressed cost of
   the currencies' localized names measured apart, so they ship with the catalog where the cost
   is small and wait where it is not; and the build, the tests and the update script's own case
   passing.
2. **The number input's adornment and precision.** Open. The adornment at either inline side with
   its accessible description, `MaxDecimalPlaces` with trailing zeros not counted and an amount
   with more places reported with its text kept, and the fewest places a number is shown with.
   Gate: tests of each, an existing number input unchanged, and the number input's Showcase page
   showing a percentage and a limited precision.
3. **The currency input.** Open. `FormaCurrencyInput` composing the number input under one field,
   its symbol, side and precision taken from the catalog and the formatting culture, its Showcase
   page and its locale files. Gate: tests of each rule, one field with one label, one set of
   messages and one set of ids; a browser pass in English, Hebrew and Spanish under a fine pointer
   and a coarse one, with the US dollar in Hebrew, the shekel in English, a yen amount and a
   negative amount; and the screen reader check gaining the currency input's scenario.
