# Forma Localization Implementation Plan

The plan for the locale packs [the localization design](localization-design.md) states, and the
record of what has landed.

## Status

Steps 1 to 5 are built. `LocalePackSource` reads a pack, `LocalePackAttribute` and
`LocalePackLoader` find one, `LocalizationOptions.LocalePacks` names them outright, and
`JsonResourceCache` lays each pack over the assembly's own files and under the host's. A project
is a pack by declaring `IsLocalePack`, which ships with the library and makes a project of nothing
but locale files embed each under its own path and carry a targets file that records the pack in
whatever references its package; a project referencing a pack directly is stamped by the root
build instead. `Forma.Blazor.Locales.Hebrew` and `Forma.Blazor.Locales.Spanish` carry the `he` and
`es` files of `Forma.Blazor`, `Forma.Blazor.Composer` and `Forma.Blazor.Explorer`, which ship no
locale files now. The Showcase references both packs, so its assembly names them, and so does the
component tests' shared context, which reads the packs its own assembly was built against rather
than its runner's; step 5 is built with that.

Three routes were proved by hand and await the gate check of step 6: the Showcase's assembly
carries both packs' names; a consumer outside the repository taking the Hebrew package from a feed
is stamped and loads the pack; and a project outside the repository with nothing but the flag and
the library package packs as a pack, with its own name in its targets file and its files under
their own paths.

## What is in place

- `LocaleSource` places every file it holds at the anchors an assembly's paths read it for, and
  `Read` returns a type's entries from the root down.
- `EmbeddedLocaleSource` reads one assembly's own resources, and `HostLocaleSource` reads a folder
  holding one subfolder per assembly, laid out by the root namespace each assembly declares and
  carrying no config of its own.
- `JsonResourceCache` composes the sources for a type in one culture, and `JsonStringLocaleFactory`
  builds it from `LocalizationOptions`.
- `he` and `es` sat beside the components they localize until step 4 moved them, in
  `Forma.Blazor/Components/{Category}/.locales/`, in `Forma.Blazor.Composer/.locales/` and in
  `Forma.Blazor.Explorer/.locales/`. The Showcase and the AddOns carry their own files the same way
  and keep them.

## Steps

### 1. The pack source

`LocalePackSource` in `Forma.Framework.Localization.Files`, a `LocaleSource` over an assembly's
manifest resources reading what a host's folder reads: `.` between folders,
`LocalePath.HostDefaults`, the root directory an assembly's own name, and a config synthesized from
the root namespace that assembly declares. A pack's resource names are each file's path inside the
pack with `/` made `.`.

`LocalePackAttribute` in `Forma.Framework.Localization` names one pack and may be written more than
once; `LocalePackLoader` reads the names an assembly carries and loads each, reporting one it cannot
load rather than failing the host. `LocalizationOptions.LocalePacks` names the packs outright, and
is null to read the entry assembly's attributes and empty to read none.

`JsonResourceCache` builds a source over each pack and reads them between the assembly's own files
and the host's; `JsonStringLocaleFactory` resolves what the options leave null.

### 2. The build a pack brings

A project is a pack because it says `<IsLocalePack>true</IsLocalePack>`, not because of where it
sits, so any project anywhere can be one. What the flag does ships with the library, as
`build/Forma.Framework.Localization.targets` in that package: no compile items, every `*.json`
beneath the project but the build's own under `bin/` and `obj/` embedded under its own path as its
resource name, packable, and a consumer targets file packed as `build/{PackageId}.targets`. That
second file, shipped in every pack's package, stamps the project referencing the pack with the
attribute and roots the pack for the trimmer. A pack declares `LocalePackCulture`, which the build
requires, refusing a file of any other culture, and which the assembly carries as metadata.

The same targets file embeds an assembly's `.locale.json` whenever one sits at the project root,
for every project and not only a pack, so no project names its config file any more; the root
`Directory.Build.targets` imports the file for every project here for that reason.

This repository's own packs reference the library as a project rather than as a package, which
imports no targets file, so the root `Directory.Build.targets` imports the same file when the flag
is set; one file, two routes in. A pack's own project file then states the flag, the reference and
its description.

The Showcase references a pack as a project too, so the consumer targets are not imported there
either. The root `Directory.Build.targets` asks each referenced project whether it is a pack and
stamps the attribute for each that says so, matching no name.

### 3. The packs

`Forma.Blazor.Locales.Hebrew` and `Forma.Blazor.Locales.Spanish` in `src/Forma.Locales/`, each
carrying `Forma.Blazor`, `Forma.Blazor.Composer` and `Forma.Blazor.Explorer`.

### 4. The move

The `he` files of those three projects move into `Forma.Blazor.Locales.Hebrew` and the `es` files
into `Forma.Blazor.Locales.Spanish`, each under a folder named for the assembly it belongs to. The
three projects keep their `.locale.json` where they have one, since a pack lays an assembly's
folder out by the root namespace the assembly declares, and lose the `EmbeddedResource` items for
files they no longer hold.

The Showcase keeps every locale file it has, embedded in its own assembly exactly as today, and so
do the AddOns. What they do is not a legacy arrangement, and the Showcase is where the repository
demonstrates that an assembly's own files and a pack are read together.

### 5. The consumers

The Showcase references both packs, so what it renders in Hebrew and Spanish comes from them. A test
that reads a string from a pack names the pack through `LocalizationOptions`, since a test's entry
assembly is its runner's business.

### 6. The tests

Over the source, in `Forma.Framework.Localization.Tests`, against a fixture pack's resources: a file
found at a folder's anchor and at a type's, a file for an assembly whose root namespace is not its
name, and a culture the pack holds nothing for yielding nothing.

Over the layering, in `Forma.Framework.Localization.Json.Tests`: a pack read where the assembly
carries nothing, a pack overriding what the assembly ships, a host's file overriding a pack's, two
packs read in the order given, and a pack named in an attribute but missing reported rather than
thrown past.

Over the build, in the gate: the Showcase is built and its `LocalePack` attributes read, so the
targets file being imported, the attribute being generated and the name being right are proved on a
real consumer rather than reasoned about.

### 7. The prose

`README.md` gains the locale packs a host installs. The `forma-localizing` skill states where a
library's locale files go now, and that a pack is one of the places they may go.
