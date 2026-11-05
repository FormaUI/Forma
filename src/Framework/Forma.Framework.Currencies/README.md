# Forma.Framework.Currencies

What is known about a currency apart from any culture: the ISO 4217 codes of the currencies in
use, the number of decimal places an amount in each usually carries, and the symbol each is written
with in a given culture. It suits anything that shows or edits money in a currency the application
names, whatever culture the reader formats numbers in: a price field, a table of totals, a report.

## Install

```pwsh
dotnet add package formaui-net.Framework.Currencies --prerelease
```

The catalog is static and registers nothing.

## Reading the catalog

```csharp
using System.Globalization;
using Forma.Framework.Currencies;

var hebrew = CultureInfo.GetCultureInfo("he-IL");

CurrencyCatalog.Contains("USD");               // true
CurrencyCatalog.GetMinorUnits("JPY");          // 0
CurrencyCatalog.GetSymbol("USD", hebrew);      // "$"
CurrencyCatalog.GetSymbol("USD", CultureInfo.GetCultureInfo("en-AU")); // "USD", since "$" is the Australian dollar there
```

| Member | What it gives |
| --- | --- |
| `CurrencyCatalog.Codes` | The ISO 4217 codes of the currencies in use, in order. |
| `CurrencyCatalog.Contains(code)` | Whether the code is one of them. |
| `CurrencyCatalog.GetMinorUnits(code)` | The decimal places an amount in the currency usually carries: 2 for the US dollar, 0 for the yen, 3 for the Bahraini dinar. |
| `CurrencyCatalog.GetSymbol(code, culture)` | The symbol the culture writes the currency with, or the ISO code where the culture has none. |

A code outside the currencies in use, such as a retired one, is refused with an `ArgumentException`.
The symbol is looked up from the culture through the locales it derives from, as Unicode CLDR
defines them, so a culture CLDR names differently, such as `zh-TW`, finds the locale it belongs to.

## Where the data comes from

The data is a snapshot of [Unicode CLDR](https://cldr.unicode.org) embedded in the assembly, so
nothing is fetched at run time; the release it was read from is recorded in the snapshot. CLDR's data
is under the Unicode License, whose notice the package carries in
[`THIRD-PARTY-NOTICES.md`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Currencies/THIRD-PARTY-NOTICES.md).

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
