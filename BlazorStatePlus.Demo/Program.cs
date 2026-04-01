using BlazorStatePlus.Demo.Components;
using BlazorStatePlus.Demo.Services;
using BlazorStatePlus.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorStatePlus();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ReviewService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
