using Bustand.DevTools.Extensions;
using Bustand.Extensions;
using Bustand.Sample.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Razor component services for all render modes
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Register Bustand stores (scans both Server and Client assemblies)
builder.Services.AddBustand(options =>
{
    options.ScanAssemblyContaining<Program>();
    options.ScanAssemblyContaining<Bustand.Sample.Client._Imports>();
});

// DevTools in development only
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddBustandDevTools();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();  // CRITICAL: Must be before AddInteractiveWebAssemblyRenderMode
app.UseAntiforgery();

// Map Razor components with all render modes
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(Bustand.Sample.Client._Imports).Assembly,
        typeof(Bustand.DevTools.Components.DevToolsPage).Assembly);

app.Run();
