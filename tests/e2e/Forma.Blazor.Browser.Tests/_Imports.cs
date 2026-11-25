global using System;
global using System.Threading.Tasks;

global using Forma.Tests.Hosts;

global using Microsoft.Playwright;

global using Xunit;

[assembly: AssemblyFixture(typeof(Forma.Blazor.Browser.Tests.BrowserHost))]
[assembly: Forma.Blazor.Browser.Tests.FailOnPolicyViolation]
