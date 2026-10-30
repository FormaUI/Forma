using Forma.Blazor;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddFormaUIWebAssembly();
await builder.Build().RunAsync().ConfigureAwait(false);
