using TheBlazorState.Demo.Components;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;
using TheBlazorState.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddTheBlazorState();

builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<StateInspectorService>();
builder.Services.AddScoped<ProjectState>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<AppJsModule>();
builder.Services.AddScoped<CrossTabState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

namespace TheBlazorState.Demo { public class Program { } }
