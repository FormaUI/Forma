using Forma.Blazor;
using Forma.Blazor.Browser.Components.Client;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddFormaUIWebAssembly();
builder.Services.AddCheckedComponents();
await builder.Build().RunAsync().ConfigureAwait(false);
