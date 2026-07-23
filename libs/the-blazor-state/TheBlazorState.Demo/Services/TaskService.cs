using TheBlazorState.Demo.Models;

namespace TheBlazorState.Demo.Services;

public class TaskService
{
    private static readonly Dictionary<int, BoardData> Boards = new()
    {
        [1] = new BoardData(
            ToDo: [
                new TaskItem(1, "Write blog post", Priority.High, "Alice", "AL"),
                new TaskItem(2, "Design social media graphics", Priority.Medium, "Bob", "BO"),
                new TaskItem(3, "Plan Q3 campaign", Priority.Low, "Carol", "CA"),
                new TaskItem(4, "Update landing page copy", Priority.Medium, "Alice", "AL")
            ],
            InProgress: [
                new TaskItem(5, "A/B test email subject lines", Priority.High, "Bob", "BO"),
                new TaskItem(6, "Prepare press kit", Priority.Medium, "Carol", "CA"),
                new TaskItem(7, "Review analytics report", Priority.Low, "Alice", "AL")
            ],
            Done: [
                new TaskItem(8, "Launch newsletter", Priority.High, "Bob", "BO"),
                new TaskItem(9, "Set up tracking pixels", Priority.Medium, "Carol", "CA"),
                new TaskItem(10, "Redesign email template", Priority.Low, "Alice", "AL"),
                new TaskItem(11, "Create brand guidelines", Priority.High, "Bob", "BO"),
                new TaskItem(12, "Audit competitor content", Priority.Medium, "Carol", "CA")
            ]),
        [2] = new BoardData(
            ToDo: [
                new TaskItem(20, "Implement auth middleware", Priority.High, "Dave", "DA"),
                new TaskItem(21, "Add rate limiting", Priority.High, "Eve", "EV"),
                new TaskItem(22, "Write integration tests", Priority.Medium, "Frank", "FR"),
                new TaskItem(23, "Set up monitoring", Priority.Low, "Dave", "DA"),
                new TaskItem(24, "Optimize database queries", Priority.High, "Eve", "EV"),
                new TaskItem(25, "Migrate to .NET 10", Priority.Medium, "Frank", "FR")
            ],
            InProgress: [
                new TaskItem(26, "Build REST API v2", Priority.High, "Dave", "DA"),
                new TaskItem(27, "Refactor data layer", Priority.Medium, "Eve", "EV"),
                new TaskItem(28, "Configure CI/CD pipeline", Priority.High, "Frank", "FR"),
                new TaskItem(29, "Implement caching strategy", Priority.Medium, "Dave", "DA")
            ],
            Done: [
                new TaskItem(30, "Set up project structure", Priority.High, "Eve", "EV"),
                new TaskItem(31, "Create development environment", Priority.Medium, "Frank", "FR")
            ]),
        [3] = new BoardData(
            ToDo: [
                new TaskItem(40, "Design onboarding flow", Priority.High, "Grace", "GR"),
                new TaskItem(41, "Create icon set", Priority.Medium, "Hank", "HA"),
                new TaskItem(42, "Prototype mobile layout", Priority.Low, "Grace", "GR")
            ],
            InProgress: [
                new TaskItem(43, "Redesign settings page", Priority.Medium, "Hank", "HA"),
                new TaskItem(44, "Update color palette", Priority.High, "Grace", "GR")
            ],
            Done: [
                new TaskItem(45, "Design system foundations", Priority.High, "Hank", "HA"),
                new TaskItem(46, "Component library v1", Priority.High, "Grace", "GR"),
                new TaskItem(47, "Accessibility audit", Priority.Medium, "Hank", "HA"),
                new TaskItem(48, "User research interviews", Priority.Low, "Grace", "GR"),
                new TaskItem(49, "Wireframe dashboard", Priority.Medium, "Hank", "HA"),
                new TaskItem(50, "Style guide documentation", Priority.Low, "Grace", "GR"),
                new TaskItem(51, "Logo redesign", Priority.High, "Grace", "GR")
            ])
    };

    public async Task<BoardData> GetBoardAsync(int projectId)
    {
        await Task.Delay(200);
        return Boards.GetValueOrDefault(projectId) ?? Boards[1];
    }
}
