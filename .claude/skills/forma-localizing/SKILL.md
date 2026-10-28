---
name: forma-localizing
description: The localization standard — reading library strings through the base's L, sharing a composite's file through LocalizationSourceType, shipping a locale file per language for each category folder, sectioned by component and keyed by English source text, passing interpolation holes positionally in PascalCase, the Title Case versus sentence case rule that separates labels from prose across every Latin-script language, and linting the files against their call sites. Use when adding or changing any user-facing string, editing a locale file, or naming an interpolation hole.
---

# Localization

- Do read a component's library-owned strings through the base's `L["The English sentence"]`; every component has `L` from the component base, resolved from its own type, so nothing is injected.
- Do override `LocalizationSourceType` on a part that reads a composite's file rather than its own, `=> typeof(FormaX)` on the parts of `FormaX`, so those parts share one locale pair.
- Do inject `IStringLocale<T>` only where there is no base: a service, or a showcase page; it reads the ambient culture as it renders, so a component holding one renders again on each change of the culture state, as the showcase's pages do through their shared base.
- Do ship the library's locale files in the locale pack of their language, where the repository's structure document places the packs, under a folder named for the assembly they localize and one file per category folder named for the culture alone, `Forma.Blazor/Components/Bars/he.json`, holding a section per component keyed by that English source text, `"FormaToolbar": { "Bold": "…" }`; the product and the components that are their own projects ship no locale files of their own, a new language is a new pack declaring `IsLocalePack` and its `LocalePackCulture`, and there is no neutral file, because an absent translation falls back to the key itself.
- Do keep an AddOn's locale files in the AddOn, in its own `.locales` folder, since an AddOn is a host opt-in already and its strings arrive with it.
- Do keep the file's folder in step with the components' namespace and each section's name with its component's type name, since a resource is resolved from the type's full name and the folder plus the section must spell it; a generic component resolves to its definition's name, so `FormaX<TValue>` reads the `FormaX` section.
- Do give a type a file of its own, named for the type among its category folder's files, `Forma.Blazor/Components/Bars/FormaX.he.json`, only where its strings must not share a file, since the file closer to the type wins over the category's file for the keys it carries; a folder is a namespace, never a type.
- Do put a translation every component in a category shares under the file's `$shared` section, right after the metadata entry, rather than repeating it in each section; a section's own entry still wins where one component words it differently.
- Do double the `$` on a key that is `$` followed by one lowercase word and nothing else, `L["$$price"]` and `"$$price"` in the file, since that shape is reserved for the runtime's own entries and one it does not define fails to load; every other key starting with `$`, such as `$5 off` or `$USD`, is ordinary text.
- Do embed an AddOn's locale files in its assembly through an `EmbeddedResource` item naming the JSON files, since the SDK embeds none by default, with `WithCulture="false"` on it so a file whose name ends in its culture stays in the assembly rather than moving into a satellite beside it; the loader reads a satellite too, for a project that leaves the build's own layout in place. A pack embeds its files itself, and every project's `.locale.json` is embedded by the build, so neither is named in a project file.
- Do leave caller-supplied text such as `Title`, `Label`, and `ConfirmText` for the host to localize.
- Do format, parse and compare dates, numbers and text with the base's `CultureState.Culture`, choose direction with `CultureState.IsRightToLeft`, and run an API that reads the ambient culture, such as `DisplayAttribute.GetName()`, in `CultureState.Run`; the ambient culture's members are banned in a component project.
- Do use American English spelling for `en` localization.
- Do run the localization linter, which checks the locale files against the `L[...]` call sites that key them, before offering the work for review.
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
- Do treat Title Case as the library's own typographic style rather than any language's orthographic standard: English prose would write `Select an option`, and most Latin-script languages sentence-case titles by convention, yet every label is Title Case because it reads better and keeps one rule across the product.
- Don't Title Case a sentence, and don't sentence-case a label.
