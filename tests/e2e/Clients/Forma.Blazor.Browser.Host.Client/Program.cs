using Forma.Blazor;
using Forma.Blazor.Browser.Host.Client.Navigation;
using Forma.Framework.Culture;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The host hands the runtime its arrangement's variable, which the client declares as the server does.
builder.Services.AddFormaWebAssemblyIntegration(options => options.CultureArrangement = Environment.GetEnvironmentVariable("FORMA_BROWSER_ARRANGEMENT") == "independent"
    ? CultureArrangement.Independent
    : CultureArrangement.Coordinated);
builder.Services.AddScoped<NavigationRecorder>();
await builder.Build().RunAsync().ConfigureAwait(false);
