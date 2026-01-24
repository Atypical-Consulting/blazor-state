using Bustand.DevTools.Services;
using Xunit;

namespace Bustand.Tests.DevTools;

/// <summary>
/// Unit tests for <see cref="DiffService"/>.
/// </summary>
public class DiffServiceTests
{
    private readonly DiffService _service;

    public DiffServiceTests()
    {
        _service = new DiffService();
    }

    [Fact]
    public void ComputeDiff_IdenticalStates_ReturnsEqual()
    {
        // Arrange
        var state = new TestState("value", 42);

        // Act
        var result = _service.ComputeDiff(state, state);

        // Assert
        Assert.True(result.AreEqual);
        Assert.Empty(result.Differences);
    }

    [Fact]
    public void ComputeDiff_ModifiedProperty_ReturnsModified()
    {
        // Arrange
        var oldState = new TestState("value", 42);
        var newState = new TestState("value", 100);

        // Act
        var result = _service.ComputeDiff(oldState, newState);

        // Assert
        Assert.False(result.AreEqual);
        Assert.Contains(result.Differences, d => d.Type == DiffType.Modified);
    }

    [Fact]
    public void ComputeDiff_NullOldState_ReturnsAdded()
    {
        // Arrange
        var newState = new TestState("value", 42);

        // Act
        var result = _service.ComputeDiff(null, newState);

        // Assert
        Assert.Contains(result.Differences, d => d.Type == DiffType.Added);
        Assert.Single(result.Differences);
        Assert.Equal("null", result.OldStateJson);
    }

    [Fact]
    public void ComputeDiff_NullNewState_ReturnsRemoved()
    {
        // Arrange
        var oldState = new TestState("value", 42);

        // Act
        var result = _service.ComputeDiff(oldState, null);

        // Assert
        Assert.Contains(result.Differences, d => d.Type == DiffType.Removed);
        Assert.Single(result.Differences);
        Assert.Equal("null", result.NewStateJson);
    }

    [Fact]
    public void ComputeDiff_BothNull_ReturnsEmpty()
    {
        // Act
        var result = _service.ComputeDiff(null, null);

        // Assert
        Assert.True(result.AreEqual);
        Assert.Empty(result.Differences);
    }

    [Fact]
    public void ComputeDiff_SerializesOldStateToJson()
    {
        // Arrange
        var oldState = new TestState("old", 1);
        var newState = new TestState("new", 2);

        // Act
        var result = _service.ComputeDiff(oldState, newState);

        // Assert
        Assert.Contains("old", result.OldStateJson);
    }

    [Fact]
    public void ComputeDiff_SerializesNewStateToJson()
    {
        // Arrange
        var oldState = new TestState("old", 1);
        var newState = new TestState("new", 2);

        // Act
        var result = _service.ComputeDiff(oldState, newState);

        // Assert
        Assert.Contains("new", result.NewStateJson);
    }

    [Fact]
    public void ComputeDiff_MultipleChanges_DetectsAll()
    {
        // Arrange
        var oldState = new TestState("old", 1);
        var newState = new TestState("new", 2);

        // Act
        var result = _service.ComputeDiff(oldState, newState);

        // Assert
        Assert.False(result.AreEqual);
        Assert.True(result.Differences.Count >= 2);
    }

    [Fact]
    public void ComputeDiff_DifferenceContainsPropertyPath()
    {
        // Arrange
        var oldState = new TestState("value", 42);
        var newState = new TestState("value", 100);

        // Act
        var result = _service.ComputeDiff(oldState, newState);

        // Assert
        var diff = result.Differences.First(d => d.Type == DiffType.Modified);
        Assert.NotNull(diff.PropertyPath);
        Assert.NotEmpty(diff.PropertyPath);
    }

    [Fact]
    public void ComputeDiff_DifferenceContainsOldValue()
    {
        // Arrange
        var oldState = new TestState("value", 42);
        var newState = new TestState("value", 100);

        // Act
        var result = _service.ComputeDiff(oldState, newState);

        // Assert
        var diff = result.Differences.First(d => d.Type == DiffType.Modified);
        Assert.Contains("42", diff.OldValue);
    }

    [Fact]
    public void ComputeDiff_DifferenceContainsNewValue()
    {
        // Arrange
        var oldState = new TestState("value", 42);
        var newState = new TestState("value", 100);

        // Act
        var result = _service.ComputeDiff(oldState, newState);

        // Assert
        var diff = result.Differences.First(d => d.Type == DiffType.Modified);
        Assert.Contains("100", diff.NewValue);
    }

    [Fact]
    public void ComputeDiff_NestedObject_DetectsChanges()
    {
        // Arrange
        var oldState = new NestedState(new InnerState("old"));
        var newState = new NestedState(new InnerState("new"));

        // Act
        var result = _service.ComputeDiff(oldState, newState);

        // Assert
        Assert.False(result.AreEqual);
        Assert.Contains(result.Differences, d => d.Type == DiffType.Modified);
    }

    [Fact]
    public void ComputeDiff_Collection_DetectsChanges()
    {
        // Arrange
        var oldState = new CollectionState(new List<string> { "a", "b" });
        var newState = new CollectionState(new List<string> { "a", "c" });

        // Act
        var result = _service.ComputeDiff(oldState, newState);

        // Assert
        Assert.False(result.AreEqual);
    }

    [Fact]
    public void DiffResult_Empty_HasCorrectDefaults()
    {
        // Act
        var empty = DiffResult.Empty;

        // Assert
        Assert.True(empty.AreEqual);
        Assert.Empty(empty.Differences);
        Assert.Empty(empty.OldStateJson);
        Assert.Empty(empty.NewStateJson);
    }

    [Fact]
    public void DiffItem_HasCorrectProperties()
    {
        // Arrange
        var item = new DiffItem("Path", DiffType.Modified, "old", "new");

        // Assert
        Assert.Equal("Path", item.PropertyPath);
        Assert.Equal(DiffType.Modified, item.Type);
        Assert.Equal("old", item.OldValue);
        Assert.Equal("new", item.NewValue);
    }

    [Fact]
    public void DiffType_HasAllExpectedValues()
    {
        // Assert
        Assert.Equal(3, Enum.GetValues<DiffType>().Length);
        Assert.Contains(DiffType.Added, Enum.GetValues<DiffType>());
        Assert.Contains(DiffType.Removed, Enum.GetValues<DiffType>());
        Assert.Contains(DiffType.Modified, Enum.GetValues<DiffType>());
    }

    private record TestState(string Text, int Number);
    private record InnerState(string Value);
    private record NestedState(InnerState Inner);
    private record CollectionState(List<string> Items);
}
