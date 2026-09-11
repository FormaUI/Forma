---
name: forma-localizing
description: Forma's localization standard — reading library strings through the base's L, sharing a feature's file through LocalizationSourceType, shipping the FormaX.he.json and FormaX.es.json pair keyed by English source text, passing interpolation holes positionally in PascalCase, and the Title Case versus sentence case rule that separates labels from prose across every Latin-script language. Use when adding or changing any user-facing string, editing a translation file, or naming an interpolation hole.
---

# Localization

- Do read a component's library-owned strings through the base's `L["The English sentence"]`; every component has `L` from `FormaComponentBase`, resolved from its own type, so nothing is injected.
- Do override `LocalizationSourceType` on a part that reads a feature's file rather than its own — `=> typeof(FormaExplorer)` on the explorer's parts, `typeof(FormaComposer)` on the composer's — so those parts share one translation pair.
- Do inject `IStringLocale<T>` only where there is no base: a service, or a Showcase page.
- Do ship `FormaX.he.json` and `FormaX.es.json` beside the component, keyed by that English source text; there is no neutral file, because an absent translation falls back to the key itself.
- Do keep the file's name and folder in step with the type's, since the resource is resolved from the type's full name and the JSON's path must match it; a generic component resolves to its definition's files, so `FormaSelect<TValue>` reads `FormaSelect.he.json`.
- Do leave caller-supplied text such as `Title`, `Label`, and `ConfirmText` for the host to localize.
- Do format dates and numbers with `CultureInfo.CurrentCulture` and use `TextInfo.IsRightToLeft` when direction changes behaviour.
- Do use American English spelling for `en` localization.
- Don't hardcode user-facing component strings.
- Don't localize text supplied by the caller a second time inside the library.

## Interpolation

- Do pass localization interpolation values positionally; never bind localization holes to C# variable names.
- Do treat hole names as part of the localized message, chosen for readability and meaning rather than to mirror identifiers in code.
- Do name every localization hole in PascalCase.
- Do choose hole names that make the message read naturally to translators.
- Don't rename a localization hole merely because the corresponding variable, property, or parameter was renamed.

For example:

```csharp
L["The {Amount} of {Files}", amount, files]
```

## Capitalization

A string is a **label** when it names a thing the user acts on or reads as a heading — buttons, menu items, column headers, tab titles, field labels, select options, and the accessible names of icon-only controls. Everything else is **prose**: sentences, validation messages, empty-state copy, placeholders, tooltips that explain rather than name, and status readouts that combine a word with a value.

- Do write labels in Title Case: `New Folder`, `Sort Ascending`, `Select an Option`, `All Media`.
- Do capitalize the first and last word of a label, plus every noun, verb, adjective, adverb, and pronoun between them.
- Do leave articles, coordinating conjunctions, and prepositions lowercase inside a label, so `Go to Page` and `Folders in {Folder}` keep their connectives down.
- Do write prose in sentence case and punctuate it normally: `Drop files here or click to browse`, `"{Name}" isn't an accepted file type.`
- Do apply the identical rule to every Latin-script language, capitalizing the same word positions the English label does: `All Media` is `Todos los Medios`, never `Todos Los Medios`, and `Sort By` is `Ordenar Por`.
- Do leave a script without letter case alone, such as Hebrew.
- Do treat Title Case as Forma's own typographic style rather than any language's orthographic standard: English prose would write `Select an option`, and most Latin-script languages sentence-case titles by convention, yet every label is Title Case because it reads better and keeps one rule across the product.
- Don't Title Case a sentence, and don't sentence-case a label.

## Linting

The sibling `Forma.Locali` repository ships `locali`, which checks translation files against the `L[...]` call sites that key them.
