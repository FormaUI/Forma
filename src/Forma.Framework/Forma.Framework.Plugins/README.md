# Forma.Framework.Plugins

A registry for one feature's extensions: what to create, how to configure it, what it sits
under, and who disposes it. A feature declares its own plugin contract, hands the registry that
contract as its type argument, and gets registration, construction, ordering and disposal from
this package rather than writing them again.

Everything here is free of Blazor and of any container, so a feature's registrations and its
tests need no renderer and no service collection beyond an `IServiceProvider`.

## Install

```pwsh
dotnet add package FormaUI.NET-Framework.Plugins
```

## The contracts

| Type | What it is |
| --- | --- |
| `IPlugin` | An extension identified within its owning registry. A feature's own contract derives from it. |
| `IPluginConfigurable<TOptions>` | Declares the options record a plugin's constructor takes, so a registration naming the wrong pair does not compile. |
| `PluginRegistry<TPlugin>` | What one owner registers, in order, with whatever each plugin registers in turn. |
| `PluginScope<TPlugin>` | One owner's constructed plugins, and the disposal of whichever of them it created. |
| `PluginInstance<TPlugin>` | One constructed plugin, what it was registered under, and who governs how long it lives. |

## Registering

A registry records what to create rather than what was created, so one description serves every
owner that activates it and no two owners share a plugin.

```csharp
var registry = new PluginRegistry<IChartPlugin>()
    .Add<LinePlugin>(line => line
        .Add<LegendPlugin>()
        .Add<GridPlugin, GridOptions>(options => options.Step = 10))
    .Add<ExportPlugin>(_ => new ExportPlugin(endpoint))
    .TryAdd<TooltipPlugin>();
```

- `Add<TImplementation>` registers a plugin built from its constructor, optionally with whatever
  is registered under it.
- `Add<TImplementation, TOptions>` registers one that takes options, and fills a fresh options
  record for every owner that activates the registry.
- `Add<TImplementation>(factory)` registers one the host creates itself.
- `TryAdd<TImplementation>` registers a fallback, which is left out when the same owner registers
  a plugin of that type itself — however early or late the fallback was asked for.

Registering a type a second time replaces the first registration **where it stood**, dropping
whatever the replaced one had registered under it, so re-registering one plugin leaves the rest
in place.

## Activating

```csharp
await using var scope = registry.Activate(services);

foreach (var plugin in scope.Plugins)
{
    // …
}
```

Activation lays the plugins out in registration order, each followed by whatever sits under it,
and resolves every constructor parameter in turn from:

1. a plugin registered beside it — so everything naming the same plugin is handed the single
   instance of it,
2. the registration's own options record,
3. the host's services,
4. the parameter's default value.

A parameter none of those can supply, a plugin with more than one public constructor, and a
cycle between two plugins each throw an `InvalidOperationException` naming the plugin and the
parameter.

The same plugin type registered in two places is two plugins, one per place, each with its own
options.

## Lifetime

Disposing the scope disposes every plugin it constructed, `IAsyncDisposable` first and
`IDisposable` otherwise. What a factory supplied is left alone, because a factory answering one
instance to every owner is the host's to govern; `PluginInstance.IsAutonomous` says which is
which.

Being a plugin's owner says nothing about disposing it: the tree and the disposal are separate
questions, so a plugin registered under another may still govern its own lifetime, and one at
the root may not.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
