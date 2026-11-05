global using Forma.Framework.Culture.Blazor.WebView.Tests.Support;

global using Xunit;

// An application sets the process's default cultures, which every test here reads, so the tests run one at a time.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]
