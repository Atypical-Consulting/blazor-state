using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bustand.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register Bustand stores for client-side
builder.Services.AddBustand(options =>
{
    options.ScanAssemblyContaining<Program>();
});

await builder.Build().RunAsync();
