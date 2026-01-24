using Bustand.Persistence;

namespace Bustand.Tests.Persistence;

public class DebouncedWriterTests
{
    private record TestState(int Value);

    [Fact]
    public async Task QueueWrite_SingleCall_WritesAfterDebounce()
    {
        // Arrange
        var writtenStates = new List<TestState>();
        var writer = new DebouncedWriter<TestState>(
            async state =>
            {
                writtenStates.Add(state);
                await Task.CompletedTask;
            },
            debounceMs: 50);

        // Act
        writer.QueueWrite(new TestState(1));
        await Task.Delay(100); // Wait for debounce

        // Assert
        Assert.Single(writtenStates);
        Assert.Equal(1, writtenStates[0].Value);

        writer.Dispose();
    }

    [Fact]
    public async Task QueueWrite_RapidCalls_BatchesIntoSingleWrite()
    {
        // Arrange
        var writtenStates = new List<TestState>();
        var writer = new DebouncedWriter<TestState>(
            async state =>
            {
                writtenStates.Add(state);
                await Task.CompletedTask;
            },
            debounceMs: 100);

        // Act - rapid calls within debounce window
        writer.QueueWrite(new TestState(1));
        await Task.Delay(20);
        writer.QueueWrite(new TestState(2));
        await Task.Delay(20);
        writer.QueueWrite(new TestState(3));

        await Task.Delay(200); // Wait for debounce

        // Assert - only last state should be written
        Assert.Single(writtenStates);
        Assert.Equal(3, writtenStates[0].Value);

        writer.Dispose();
    }

    [Fact]
    public async Task QueueWrite_CallsWithGap_WritesBoth()
    {
        // Arrange
        var writtenStates = new List<TestState>();
        var writer = new DebouncedWriter<TestState>(
            async state =>
            {
                writtenStates.Add(state);
                await Task.CompletedTask;
            },
            debounceMs: 50);

        // Act - calls with gap larger than debounce
        writer.QueueWrite(new TestState(1));
        await Task.Delay(100); // Wait for first debounce
        writer.QueueWrite(new TestState(2));
        await Task.Delay(100); // Wait for second debounce

        // Assert - both states should be written
        Assert.Equal(2, writtenStates.Count);
        Assert.Equal(1, writtenStates[0].Value);
        Assert.Equal(2, writtenStates[1].Value);

        writer.Dispose();
    }

    [Fact]
    public async Task FlushAsync_ForcesImmediateWrite()
    {
        // Arrange
        var writtenStates = new List<TestState>();
        var writer = new DebouncedWriter<TestState>(
            async state =>
            {
                writtenStates.Add(state);
                await Task.CompletedTask;
            },
            debounceMs: 1000); // Long debounce

        // Act
        writer.QueueWrite(new TestState(1));
        await writer.FlushAsync(); // Force immediate write

        // Assert
        Assert.Single(writtenStates);
        Assert.Equal(1, writtenStates[0].Value);

        writer.Dispose();
    }

    [Fact]
    public async Task FlushAsync_NoPending_DoesNothing()
    {
        // Arrange
        var writeCount = 0;
        var writer = new DebouncedWriter<TestState>(
            async _ =>
            {
                writeCount++;
                await Task.CompletedTask;
            },
            debounceMs: 50);

        // Act
        await writer.FlushAsync();

        // Assert
        Assert.Equal(0, writeCount);

        writer.Dispose();
    }

    [Fact]
    public void Dispose_PreventsSubsequentWrites()
    {
        // Arrange
        var writeCount = 0;
        var writer = new DebouncedWriter<TestState>(
            async _ =>
            {
                writeCount++;
                await Task.CompletedTask;
            },
            debounceMs: 50);

        // Act
        writer.Dispose();
        writer.QueueWrite(new TestState(1));

        // Assert - no writes after dispose
        Assert.Equal(0, writeCount);
    }

    [Fact]
    public void Constructor_NegativeDebounce_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DebouncedWriter<TestState>(_ => Task.CompletedTask, debounceMs: -1));
    }

    [Fact]
    public void Constructor_NullAction_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DebouncedWriter<TestState>(null!, debounceMs: 50));
    }
}
