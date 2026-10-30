# Forma Currency Input Design

## Scope

`FormaCurrencyInput`, a field for an amount of money in a currency the application names, the
number input's two capabilities it is built on, an adornment beside the number and a limit on
its decimal places, and `Forma.Framework.Currencies`, the currency metadata it reads.

## Purpose

An amount is typed as the formatting culture writes a number, beside the symbol of the currency
it is in, and an amount the field cannot take is reported, never rounded or written. The currency
is the application's: a store pricing in US dollars shows dollars to a reader in any culture, and
the reader's culture decides only how the number and the symbol are written.

## Decision summary

- **What binds.** The currency input binds a `decimal` or a nullable one. A value that cannot be
  empty is required, and a nullable one is required where the host sets `Required`, as the number
  input's are.
- **The application names the currency.** `CurrencyCode` is the ISO 4217 code of the amount's
  currency, `USD` or `ILS`, and is required. The formatting culture, the culture state's `Culture`,
  writes and reads the number and decides where the symbol stands and which symbol it is; it
  never chooses the currency.
- **Currency metadata is its own concern.** `Forma.Framework.Currencies` holds what is known about
  a currency apart from any culture, through `CurrencyCatalog`: the ISO 4217 codes of the
  currencies in use, each one's usual minor units, and its symbol in a given formatting culture,
  with the ISO code where no locale has a symbol. A retired code is not a currency in use, and the
  catalog refuses it. The symbol is found through CLDR's locale inheritance, `he-IL` to `he` to the
  root, starting from the nearest of the cultures the formatting culture derives from that CLDR
  has a locale for, so `zh-TW` starts from `zh-Hant` rather than from `zh`.
  The data is a versioned snapshot of CLDR, generated ahead of time by an update script and shipped
  with the assembly; nothing is fetched at run time. It holds every locale's symbols, which
  compress to a few kilobytes, and no currency's localized name, since the names would cost over
  twenty times as much. The catalog states facts: it neither
  validates, rounds, steps nor converts an amount, and a host reads it directly, with no contract
  standing in for it.
- **The symbol is the currency's in the reader's culture.** A symbol identifies a currency only
  in context: `$` stands for the US dollar, the Australian dollar and others, and CLDR gives each
  currency the symbol that tells it apart in each locale. The field shows the symbol CLDR gives
  the currency in the formatting culture, never the symbol of the reader's own currency, and never
  a narrow symbol, which CLDR reserves for contexts where the currency is already clear.
- **The symbol stands where the culture's currency pattern puts it.** The pattern says whether
  the symbol comes before the number or after it in reading order, `$1,234.50` in English and
  `1,234.50 $` in Hebrew, and whether a space parts them. Before is the field's inline start and
  after is its inline end, so the side follows reading order rather than being placed and then
  mirrored, and a Hebrew reader pricing in dollars and an English reader pricing in shekels each
  see the order their own culture writes. The number's own text keeps the direction the number
  input gives it, left to right in every culture, so the input reserves the symbol's room on its
  own left or right, chosen from the symbol's side and the culture's direction.
- **The adornment is not the number.** The symbol stands outside the editable text, so selecting,
  typing and reading the number never meet it, and the field's accessible description names the
  currency by its ISO code, since a symbol alone can stand for several currencies.
- **Negative amounts are amounts.** The field takes a negative amount unless the host sets a
  `Min`; a price that must not be negative says so through `Min`, never through the field. The
  sign belongs to the number, and the symbol keeps the side the culture's positive pattern gives
  it. A reader types the sign as a keyboard writes it, without the directional mark a
  right-to-left culture writes beside it.
- **Precision permitted and precision shown are separate.** The most decimal places an amount
  takes default to the currency's minor units, two for the US dollar and none for the yen, and
  `MaxDecimalPlaces` overrides them for a price or a rate that needs more. An amount with more
  places is reported with the text kept as typed, never rounded and never stopped at the
  keystroke, and trailing zeros do not count: `1.2300` is a two-place amount. An amount is shown
  with at least the currency's minor units, or as many places as the field takes where that is
  fewer, so a dollar amount of 12 reads `12.00`, and `12` where the field takes whole dollars.
- **The number input owns numeric editing.** Reading and writing in the formatting culture, the
  limits, the step and its keys, and the required state are the number input's. It gains two
  capabilities general to numbers, which a percentage or a unit uses as a currency does: an
  adornment at either inline side of the number, with an accessible description, and
  `MaxDecimalPlaces`, unrestricted unless a host sets it, beside the fewest places a number is
  shown with. A labeled number input with an adornment pins its label above the number, since a
  label floating over an empty field would cover a symbol at its start. A number input that sets
  none of them draws no adornment and limits no places.
- **One field, one input.** The currency input specializes the number input by composition: it
  is not an input of its own but hands its value, its binding, its label and its host surface to
  the one number input it draws. There is one input component under one field, so one label, one
  set of messages and one set of element ids, and the binding, the focus and every accessible
  attribute reach the element the reader types in.
- **What it does not do.** No conversion between currencies, no cash rounding, and no rule a
  particular form holds, such as a price that cannot be negative or a discount that is a
  percentage or an amount; the host chooses the input for the kind of value it is asking for.
