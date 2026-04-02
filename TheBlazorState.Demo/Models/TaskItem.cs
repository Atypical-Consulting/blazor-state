namespace TheBlazorState.Demo.Models;

public record TaskItem(int Id, string Title, Priority Priority, string AssigneeName, string AssigneeInitials);

public enum Priority { Low, Medium, High }
