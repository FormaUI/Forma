global using Forma.Tests.Hosts;

global using Microsoft.Playwright;

global using Xunit;

[assembly: AssemblyFixture(typeof(Forma.Examples.Smoke.Tests.ExampleHosts))]

// A Blazor Hybrid application starts a browser of its own, which would compete with the timing of the checks beside it, so the checks run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
