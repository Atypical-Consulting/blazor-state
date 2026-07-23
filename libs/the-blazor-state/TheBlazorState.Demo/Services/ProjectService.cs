using TheBlazorState.Demo.Models;

namespace TheBlazorState.Demo.Services;

public class ProjectService
{
    private static readonly List<Project> Projects =
    [
        new(1, "Marketing", "#6366f1"),
        new(2, "Engineering", "#0ea5e9"),
        new(3, "Design", "#f59e0b")
    ];

    public List<Project> GetAll() => Projects;
    public Project GetDefault() => Projects[0];
}
