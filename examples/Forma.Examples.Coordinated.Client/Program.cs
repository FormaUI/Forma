using Forma.Blazor;
using Forma.Framework.Localization.Json;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddJsonLocalization();
builder.Services.AddFormaWebAssemblyIntegration();

await builder.Build().RunAsync();
