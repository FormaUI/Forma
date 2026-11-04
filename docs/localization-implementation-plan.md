# Localization Implementation Plan

The steps that carry out the [localization design](localization-design.md)'s embedding of locale files
by the build, [embedded files](localization-design.md#embedded-files-and-resource-names) and
[host files](localization-design.md#host-files), with what each step's gate holds and what landed.

## Steps

1. ✅ **The design and this plan.** The design states the embedding, the host's folder, a Web App's
   client and the runtime's discovery, decisions L17 to L19. Gate: review.
2. ✅ **The build.** The localization package's targets embed every locale file by its `$localization`
   entry, once, leaving a file a project embeds itself to its own item; an application embeds a host's
   files under the prefix `.locales.`; a server records each referenced project that embedded a host's
   files with `LocaleReferenceAttribute`; the `LocalesPath` build property and the self-marking
   `HostLocalesAttribute` retire. Gate: build cases, each a fixture project evaluated with
   `dotnet msbuild -getItem`, which lists what a build embeds and records without building it:
   - own files in `.locales` folders, and at paths a `.locale.json` states, each under its default
     name;
   - a host's files in an application embedded under the prefix, and the same folder in a library
     left out, since only an application overrides another assembly's strings;
   - a project keeping its own files in its root `.locales` beside a host's folder there, with an item
     of its own as well, each file embedded once;
   - a file a project embeds itself keeping the name its item gives it, and one its project file copies
     beside its output left a file there, while a server's own file the Web SDK takes as content is
     embedded and kept out of the content;
   - JSON without the entry, the build output and folders whose names start with a dot left out;
   - a server recording the client it references, and recording nothing for a client with no host
     files.
3. ✅ **The runtime.** `LocaleReferenceAttribute(string assemblyName)`; the prefix beside the source
   that reads it; `LocalizationOptions.HostLocaleAssemblies` in place of `HostLocaleAssembly`; the
   application found by its entry assembly or, with none, among the loaded assemblies that embed a
   host's files, name an assembly that does, or record a pack; named assemblies read before the
   application's own, the folder on disk last; a named assembly that can't be loaded reported and left
   out. Gate: unit cases. With no entry assembly, under a source language other than French and with
   distinct French text in every source, an application whose only locales are its own host files
   resolves the exact French text its file supplies, one whose only locale is a recorded pack the
   pack's text, and a server whose only locale is a named client the client's text, so a source that
   discovery skips shows as another source's text or the key. Beside them: the precedence of a named
   assembly, the application's own and the folder; and a named assembly that can't be loaded.
4. ✅ **The repository's projects.** The add-ons, the Showcase and the examples lose their
   `EmbeddedResource` items, and the examples their `Content` items and `LocalesPath` properties; the
   Showcase keeps its host's folder beside it through its `Content` item, the on-disk folder its
   Localization page shows. The Web App examples' client strings move beside the client's components,
   and the Blazor Hybrid examples' from the folder named for their own assembly beside their own types. Gate: the full gate; the test
   WebAssembly client reads an own root file beside the host's folder in one `.locales`; the
   Coordinated example's override of one of Forma's strings shows in its Server island and its
   WebAssembly island.
5. ✅ **The prose.** The README's setup and localization sections, both localization READMEs, the
   localization guide's rule for an add-on's files, the features status and the feature history with
   its breaking changes. Gate: review.
