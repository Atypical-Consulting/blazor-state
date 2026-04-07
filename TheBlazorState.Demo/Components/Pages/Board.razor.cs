using Microsoft.AspNetCore.Components;
using TheBlazorState.Attributes;
using TheBlazorState.Demo.Models;
using TheBlazorState.Demo.Services;
using TheBlazorState.Demo.State;
using TheBlazorState.Storage;

namespace TheBlazorState.Demo.Components.Pages;

public partial class Board : ComponentBase
{
    [Inject] public ProjectState Project { get; set; } = null!;
    [Inject] private TaskService TaskService { get; set; } = null!;
    [Inject] private StateInspectorService Inspector { get; set; } = null!;

    private int _lastProjectId;

    [Persist(TimeToLive = "00:05:00")]
    public partial BoardData? BoardState { get; set; }

    partial void ConfigureState(__StateContext ctx)
    {
        _lastProjectId = Project.SelectedProject.Id;
        ctx.BoardState
            .KeySuffix(Project.SelectedProject.Id)
            .LoadFrom(async () => (BoardData?)await TaskService.GetBoardAsync(Project.SelectedProject.Id));
        ctx.BoardState.Storage = StorageStrategy.LocalStorage();
    }

    protected override async Task OnParametersSetAsync()
    {
        Inspector.Register("Board", [new("BoardState", "LocalStorage", BoardStateMeta)]);

        if (Project.SelectedProject.Id != _lastProjectId)
        {
            _lastProjectId = Project.SelectedProject.Id;
            BoardState = await TaskService.GetBoardAsync(Project.SelectedProject.Id);
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
        Priority.High => "bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400",
        Priority.Medium => "bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400",
        Priority.Low => "bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400",
        _ => "bg-canvas-100 dark:bg-canvas-800 text-canvas-600 dark:text-canvas-400"
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

}
