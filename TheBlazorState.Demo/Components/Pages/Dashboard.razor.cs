using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Models;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;
using TheBlazorState.Demo.Components.Shared;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] public ProjectState Project { get; set; } = default!;
    [Inject] private StatsService StatsService { get; set; } = default!;

    private int _lastProjectId;

    [Persist(TimeToLive = "00:02:00")]
    public partial DashboardData? Stats { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        _lastProjectId = Project.SelectedProject.Id;
        ctx.Stats
            .KeySuffix(Project.SelectedProject.Id)
            .LoadFrom(async () => (DashboardData?)await StatsService.GetDashboardAsync(Project.SelectedProject.Id));

    }

    private async Task Refresh()
    {
        Stats = await StatsService.GetDashboardAsync(Project.SelectedProject.Id);
    }

    private List<StateInspectorEntry> InspectorEntries =>
    [
        new("Stats", "PrerenderHtml (default)", StatsMeta)
    ];

    private static string FormatRelativeTime(DateTimeOffset time)
    {
        var diff = DateTimeOffset.UtcNow - time;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return $"{(int)diff.TotalDays}d ago";
    }
}
