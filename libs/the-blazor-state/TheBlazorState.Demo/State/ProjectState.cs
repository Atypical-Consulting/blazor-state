using TheBlazorState.Attributes;
using TheBlazorState.Demo.Models;

namespace TheBlazorState.Demo.State;

public partial class ProjectState
{
    [Shared]
    public partial Project SelectedProject { get; set; }

    public ProjectState()
    {
        SelectedProject = new Project(1, "Marketing", "#6366f1");
    }
}
