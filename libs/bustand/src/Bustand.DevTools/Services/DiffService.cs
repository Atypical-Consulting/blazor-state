using KellermanSoftware.CompareNetObjects;
using System.Text.Json;

namespace Bustand.DevTools.Services;

/// <summary>
/// Service for computing differences between state objects.
/// </summary>
public class DiffService
{
    private readonly CompareLogic _compareLogic;
    private readonly JsonSerializerOptions _jsonOptions;

    public DiffService()
    {
        _compareLogic = new CompareLogic
        {
            Config =
            {
                MaxDifferences = 100,
                IgnoreCollectionOrder = false,
                CompareChildren = true
            }
        };

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Computes the differences between two states.
    /// </summary>
    /// <param name="oldState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    /// <returns>A diff result with categorized changes.</returns>
    public DiffResult ComputeDiff(object? oldState, object? newState)
    {
        if (oldState == null && newState == null)
            return DiffResult.Empty;

        if (oldState == null)
            return new DiffResult
            {
                OldStateJson = "null",
                NewStateJson = SerializeState(newState),
                Differences = new List<DiffItem>
                {
                    new DiffItem("(root)", DiffType.Added, "null", SerializeState(newState))
                }
            };

        if (newState == null)
            return new DiffResult
            {
                OldStateJson = SerializeState(oldState),
                NewStateJson = "null",
                Differences = new List<DiffItem>
                {
                    new DiffItem("(root)", DiffType.Removed, SerializeState(oldState), "null")
                }
            };

        var comparison = _compareLogic.Compare(oldState, newState);

        var differences = comparison.Differences.Select(d => new DiffItem(
            PropertyPath: d.PropertyName,
            Type: DetermineType(d),
            OldValue: d.Object1Value ?? "null",
            NewValue: d.Object2Value ?? "null"
        )).ToList();

        return new DiffResult
        {
            OldStateJson = SerializeState(oldState),
            NewStateJson = SerializeState(newState),
            Differences = differences,
            AreEqual = comparison.AreEqual
        };
    }

    private DiffType DetermineType(Difference d)
    {
        if (d.Object1Value == null || d.Object1Value == "(null)")
            return DiffType.Added;
        if (d.Object2Value == null || d.Object2Value == "(null)")
            return DiffType.Removed;
        return DiffType.Modified;
    }

    private string SerializeState(object? state)
    {
        if (state == null) return "null";
        try
        {
            return JsonSerializer.Serialize(state, _jsonOptions);
        }
        catch
        {
            return state.ToString() ?? "Error serializing";
        }
    }
}

/// <summary>
/// Result of comparing two states.
/// </summary>
public class DiffResult
{
    public string OldStateJson { get; init; } = "";
    public string NewStateJson { get; init; } = "";
    public IReadOnlyList<DiffItem> Differences { get; init; } = Array.Empty<DiffItem>();
    public bool AreEqual { get; init; } = true;

    public static DiffResult Empty => new();
}

/// <summary>
/// A single difference between states.
/// </summary>
public record DiffItem(
    string PropertyPath,
    DiffType Type,
    string OldValue,
    string NewValue
);

/// <summary>
/// Type of difference.
/// </summary>
public enum DiffType
{
    Added,
    Removed,
    Modified
}
