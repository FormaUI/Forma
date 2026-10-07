# Forma Currency Input Implementation Plan

The plan for building what [the currency input design](currency-input-design.md) states: the
steps, their state, and where the build departed from the design. Section names refer to the
design.

## Steps

1. **The currency catalog.** ✅ `Forma.Framework.Currencies` with `CurrencyCatalog`, its
   snapshot generated from CLDR by an update script beside the plural rules', and its tests: a
   currency's minor units, its symbol in a culture through locale inheritance, and the ISO code
   where no locale has one. Gate: the snapshot's size measured with every locale and with CLDR's
   modern ones before deciding which locales the first release ships, and the compressed cost of
   the currencies' localized names measured apart, so they ship with the catalog where the cost
   is small and wait where it is not; and the build, the tests and the update script's own case
   passing. Landed: the assembly with `CurrencyCatalog` and its snapshot of CLDR 48.2.0,
   the update script with its task-script cases, and the tests. The snapshot holds the currencies
   CLDR records as tender today, 153 of them, their minor units where they differ from CLDR's
   default, and each locale's symbols where they differ from what the locale inherits. Its
   symbols compress to 7.5 KB with every locale and 3.8 KB with CLDR's modern ones, so every
   locale ships. The currencies' localized names would add 254 KB compressed with every locale
   and 167 KB with the modern ones, over twenty times the symbols, so they wait, and the
   accessible description names a currency by its ISO code.
2. **The number input's adornment and precision.** ✅ The adornment at either inline side with
   its accessible description, `MaxDecimalPlaces` with trailing zeros not counted and an amount
   with more places reported with its text kept, and the fewest places a number is shown with.
   Gate: tests of each, an existing number input unchanged, and the number input's Showcase page
   showing a percentage and a limited precision. Landed: `NumberAdornment` with its
   side and description, `MaxDecimalPlaces` and `MinDecimalPlaces`, their messages in Hebrew and
   Spanish, the tests, and the page's percentage and two-place weight. The number reads left to
   right in every culture, so its input reserves the adornment's room on its own left or right,
   chosen from the adornment's side and the formatting culture's direction. The browser pass held
   in English, Hebrew and Spanish under either pointer: the adornment on the side reading order
   gives it, clear of the typed text and on its line, and the number described by it.
3. **The currency input.** Open. `FormaCurrencyInput` composing the number input under one field,
   its symbol, side and precision taken from the catalog and the formatting culture, its Showcase
   page and its locale files. Gate: tests of each rule, one field with one label, one set of
   messages and one set of ids; a browser pass in English, Hebrew and Spanish under a fine pointer
   and a coarse one, with the US dollar in Hebrew, the shekel in English, a yen amount and a
   negative amount; and the screen reader check gaining the currency input's scenario. Landed, in
   review: the currency input of the element tier, handing its binding, label and host surface to
   the one number input it draws, its symbol from the catalog in the formatting culture, its side
   from the culture's currency pattern, its places from the currency's minor units or the host's
   override, and its description the ISO code; its tests, its Showcase page and its locale files;
   the authoring standard's rule for an input that only specializes another; and the number input
   reading a sign typed without the directional mark a right-to-left culture writes beside it, so
   a refund can be typed in Hebrew.
