using TheBlazorState.Demo.Models;

namespace TheBlazorState.Demo.Services;

public class StatsService
{
    private readonly TaskService _tasks;

    public StatsService(TaskService tasks) => _tasks = tasks;

    public async Task<DashboardData> GetDashboardAsync(int projectId)
    {
        var board = await _tasks.GetBoardAsync(projectId);
        var total = board.ToDo.Count + board.InProgress.Count + board.Done.Count;
        var now = DateTimeOffset.UtcNow;

        var activities = board.Done
            .Take(5)
            .Select((t, i) => new ActivityItem(t.Title, "completed", t.AssigneeName, now.AddMinutes(-(i * 37 + 12))))
            .ToList();

        return new DashboardData(total, board.InProgress.Count, board.Done.Count, activities);
    }
}
