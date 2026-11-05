# Forma.Framework.Plugins

A plugin registry for a feature that others extend. The feature declares its plugin contract,
and the registry supplies the rest: registering plugins in order and under one another,
configuring each through an options record, building every owner its own instances with their
constructor arguments resolved, and disposing what it built.

## Install

```pwsh
dotnet add package formaui-net.Framework.Plugins --prerelease
```

| Type | What it is |
| --- | --- |
| `IPlugin` | An extension with an `Id`, unique among the plugins registered beside it; a feature's contract derives from it. |
| `IPluginConfigurable<TOptions>` | Declares the options record a plugin's constructor takes. |
| `PluginRegistry<TPlugin>` | What one owner registers, in order, with whatever each plugin registers under it. |
| `PluginScope<TPlugin>` | One owner's built plugins, which it disposes. |
| `PluginInstance<TPlugin>` | One built plugin, its `Owner` in the tree, and whether it `IsAutonomous`, governing its own lifetime. |

## Declaring plugins

A plugin has one public constructor. It takes its options record, other plugins in the same
registry and the host's services:

```csharp
using Forma.Framework.Plugins;

public interface IChartPlugin : IPlugin
{
    void Draw(Chart chart);
}

public sealed class GridOptions
{
    public int Step { get; set; } = 5;
}

public sealed class GridPlugin(GridOptions options, LinePlugin line) : IChartPlugin, IPluginConfigurable<GridOptions>
{
    public string Id => "grid";

    public void Draw(Chart chart) => chart.DrawGrid(line.Bounds, options.Step);
}
```

## Registering

```csharp
var registry = new PluginRegistry<IChartPlugin>()
    .Add<LinePlugin>(line => line
        .Add<LegendPlugin>()
        .Add<GridPlugin, GridOptions>(options => options.Step = 10))
    .Add<ExportPlugin>(_ => sharedExporter)
    .TryAdd<TooltipPlugin>();
```

| Call | What it registers |
| --- | --- |
| `Add<TImplementation>(children)` | A plugin built from its constructor, with the plugins `children` registers under it. |
| `Add<TImplementation, TOptions>(configure, children)` | A plugin that takes options; `configure` fills a new options record each time the registry is activated. |
| `Add<TImplementation>(factory, children)` | A plugin the host creates from its `IServiceProvider`. |
| `TryAdd<TImplementation>()` | A fallback, used only when the same registry has no `Add` of that type. |

Registering a type again replaces the earlier registration in its place, together with whatever
was registered under it.

## Activating

`Activate` builds the registry's plugins for one owner, in registration order with each plugin
followed by those under it:

```csharp
await using var scope = registry.Activate(services);

foreach (var plugin in scope.Plugins)
{
    plugin.Draw(chart);
}
```

Each constructor parameter is taken from the first of these that supplies it:

1. a plugin in the same registry, the nearest owner first, as one instance shared by everything
   that takes it;
2. the registration's options record;
3. the host's services;
4. the parameter's default value.

A parameter none of them supplies, more than one public constructor, or two plugins taking each
other throws an `InvalidOperationException` that names the plugin. The same type registered in
two places is two plugins, each with its own options.

Disposing the scope disposes every plugin it built, through `IAsyncDisposable` or `IDisposable`.
A plugin a factory created is autonomous and stays with the host to dispose.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
