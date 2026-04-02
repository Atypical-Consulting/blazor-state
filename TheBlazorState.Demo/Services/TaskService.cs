using TheBlazorState.Demo.Models;

namespace TheBlazorState.Demo.Services;

public class TaskService
{
    private static readonly Dictionary<int, BoardData> Boards = new()
    {
        [1] = new BoardData(
            ToDo: [
                new(1, "Write blog post", Priority.High, "Alice", "AL"),
                new(2, "Design social media graphics", Priority.Medium, "Bob", "BO"),
                new(3, "Plan Q3 campaign", Priority.Low, "Carol", "CA"),
                new(4, "Update landing page copy", Priority.Medium, "Alice", "AL")
            ],
            InProgress: [
                new(5, "A/B test email subject lines", Priority.High, "Bob", "BO"),
                new(6, "Prepare press kit", Priority.Medium, "Carol", "CA"),
                new(7, "Review analytics report", Priority.Low, "Alice", "AL")
            ],
            Done: [
                new(8, "Launch newsletter", Priority.High, "Bob", "BO"),
                new(9, "Set up tracking pixels", Priority.Medium, "Carol", "CA"),
                new(10, "Redesign email template", Priority.Low, "Alice", "AL"),
                new(11, "Create brand guidelines", Priority.High, "Bob", "BO"),
                new(12, "Audit competitor content", Priority.Medium, "Carol", "CA")
            ]),
        [2] = new BoardData(
            ToDo: [
                new(20, "Implement auth middleware", Priority.High, "Dave", "DA"),
                new(21, "Add rate limiting", Priority.High, "Eve", "EV"),
                new(22, "Write integration tests", Priority.Medium, "Frank", "FR"),
                new(23, "Set up monitoring", Priority.Low, "Dave", "DA"),
                new(24, "Optimize database queries", Priority.High, "Eve", "EV"),
                new(25, "Migrate to .NET 10", Priority.Medium, "Frank", "FR")
            ],
            InProgress: [
                new(26, "Build REST API v2", Priority.High, "Dave", "DA"),
                new(27, "Refactor data layer", Priority.Medium, "Eve", "EV"),
                new(28, "Configure CI/CD pipeline", Priority.High, "Frank", "FR"),
                new(29, "Implement caching strategy", Priority.Medium, "Dave", "DA")
            ],
            Done: [
                new(30, "Set up project structure", Priority.High, "Eve", "EV"),
                new(31, "Create development environment", Priority.Medium, "Frank", "FR")
            ]),
        [3] = new BoardData(
            ToDo: [
                new(40, "Design onboarding flow", Priority.High, "Grace", "GR"),
                new(41, "Create icon set", Priority.Medium, "Hank", "HA"),
                new(42, "Prototype mobile layout", Priority.Low, "Grace", "GR")
            ],
            InProgress: [
                new(43, "Redesign settings page", Priority.Medium, "Hank", "HA"),
                new(44, "Update color palette", Priority.High, "Grace", "GR")
            ],
            Done: [
                new(45, "Design system foundations", Priority.High, "Hank", "HA"),
                new(46, "Component library v1", Priority.High, "Grace", "GR"),
                new(47, "Accessibility audit", Priority.Medium, "Hank", "HA"),
                new(48, "User research interviews", Priority.Low, "Grace", "GR"),
                new(49, "Wireframe dashboard", Priority.Medium, "Hank", "HA"),
                new(50, "Style guide documentation", Priority.Low, "Grace", "GR"),
                new(51, "Logo redesign", Priority.High, "Grace", "GR")
            ])
    };

    public async Task<BoardData> GetBoardAsync(int projectId)
    {
        await Task.Delay(200);
        return Boards.GetValueOrDefault(projectId) ?? Boards[1];
    }
}
