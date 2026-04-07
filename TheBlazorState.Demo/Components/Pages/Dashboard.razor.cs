using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Models;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] public ProjectState Project { get; set; } = null!;
    [Inject] private StatsService StatsService { get; set; } = null!;
    [Inject] private StateInspectorService Inspector { get; set; } = null!;

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

    protected override async Task OnParametersSetAsync()
    {
        Inspector.Register("Dashboard", [new("Stats", "PrerenderHtml (default)", StatsMeta)]);

        if (Project.SelectedProject.Id != _lastProjectId)
        {
            _lastProjectId = Project.SelectedProject.Id;
            Stats = await StatsService.GetDashboardAsync(Project.SelectedProject.Id);
        }
    }

    private async Task Refresh()
    {
        Stats = await StatsService.GetDashboardAsync(Project.SelectedProject.Id);
    }

    private static string FormatRelativeTime(DateTimeOffset time)
    {
        var diff = DateTimeOffset.UtcNow - time;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return $"{(int)diff.TotalDays}d ago";
    }

}
