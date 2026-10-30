using Forma.Blazor;
using Forma.Blazor.Browser.Host.Client.Navigation;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddFormaUIWebAssembly();
builder.Services.AddScoped<NavigationRecorder>();
await builder.Build().RunAsync().ConfigureAwait(false);
