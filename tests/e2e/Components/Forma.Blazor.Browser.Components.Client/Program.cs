using Forma.Blazor;
using Forma.Blazor.Browser.Components.Checks;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddCheckedComponents();
builder.Services.AddFormaWebAssemblyIntegration(options => options.InputRouting = CheckServices.InputRouting);
await builder.Build().RunAsync().ConfigureAwait(false);
