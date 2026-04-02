namespace TheBlazorState.Demo.Models;

public record DashboardData(int TotalTasks, int InProgress, int Completed, List<ActivityItem> RecentActivity);

public record ActivityItem(string TaskTitle, string Action, string Actor, DateTimeOffset Timestamp);
