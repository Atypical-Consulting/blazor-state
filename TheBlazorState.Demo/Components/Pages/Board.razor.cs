using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Abstractions;
using TheBlazorState.Demo.Models;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;
using TheBlazorState.Demo.Components.Shared;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Board : ComponentBase
{
    [Inject] public ProjectState Project { get; set; } = default!;
    [Inject] private TaskService TaskService { get; set; } = default!;

    private int _lastProjectId;

    [Persist(TimeToLive = "00:05:00")]
    public partial BoardData? BoardState { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        _lastProjectId = Project.SelectedProject.Id;
        ctx.BoardState
            .KeySuffix(Project.SelectedProject.Id)
            .LoadFrom(async () => (BoardData?)await TaskService.GetBoardAsync(Project.SelectedProject.Id));

        ((INotifyStateChanged)Project).StateChanged += OnProjectChanged;
    }

    private async void OnProjectChanged()
    {
        if (Project.SelectedProject.Id != _lastProjectId)
        {
            _lastProjectId = Project.SelectedProject.Id;
            BoardState = await TaskService.GetBoardAsync(Project.SelectedProject.Id);
            await InvokeAsync(StateHasChanged);
        }
    }

    private void MoveTask(TaskItem task, string from, string to)
    {
        if (BoardState is null) return;

        var todo = new List<TaskItem>(BoardState.ToDo);
        var inProgress = new List<TaskItem>(BoardState.InProgress);
        var done = new List<TaskItem>(BoardState.Done);

        GetList(from, todo, inProgress, done).Remove(task);
        GetList(to, todo, inProgress, done).Add(task);

        BoardState = new BoardData(todo, inProgress, done);
    }

    private static List<TaskItem> GetList(string column, List<TaskItem> todo, List<TaskItem> inProgress, List<TaskItem> done)
        => column switch
        {
            "todo" => todo,
            "inprogress" => inProgress,
            "done" => done,
            _ => todo
        };

    private static string PriorityClasses(Priority p) => p switch
    {
        Priority.High => "bg-rose-100 text-rose-700",
        Priority.Medium => "bg-amber-100 text-amber-700",
        Priority.Low => "bg-emerald-100 text-emerald-700",
        _ => "bg-gray-100 text-gray-600"
    };

    private static string AvatarColor(string initials) =>
        (initials[0] % 5) switch
        {
            0 => "bg-indigo-500",
            1 => "bg-emerald-500",
            2 => "bg-amber-500",
            3 => "bg-rose-500",
            _ => "bg-sky-500"
        };

    private List<StateInspectorEntry> InspectorEntries =>
    [
        new("BoardState", "PrerenderHtml (default)", BoardStateMeta)
    ];
}
