namespace TheBlazorState.Demo.Models;

public record BoardData(List<TaskItem> ToDo, List<TaskItem> InProgress, List<TaskItem> Done);
