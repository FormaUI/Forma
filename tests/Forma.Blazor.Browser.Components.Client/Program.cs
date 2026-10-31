using Forma.Blazor;
using Forma.Blazor.Browser.Components.Client;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddCheckedComponents();
builder.Services.AddFormaUIWebAssembly(options => options.InputRouting = CheckServices.InputRouting);
await builder.Build().RunAsync().ConfigureAwait(false);
