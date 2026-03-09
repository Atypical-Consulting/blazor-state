using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reactif.ConsoleApp.Config;
using Reactif.ConsoleApp.Services.FileWatchers;

namespace Reactif.Tests;

public class ReactiveFileWatcherTests : IDisposable
{
    private readonly IOptions<AppConfiguration> _options
        = Options.Create(new AppConfiguration
        {
            InputDirectory = GetTestDirectory(),
            OutputDirectory = null
        });

    public void Dispose()
    {
        // Delete the test directory
        var testDirectory = GetTestDirectory();
        Directory.Delete(testDirectory, true);
    }
    
    [Fact]
    public async Task ShouldGetInitialFiles()
    {
        // Arrange
        var path = GetTestDirectory();
        var logger = NullLogger<TextFileWatcher>.Instance;

        // Create some files in the directory
        var expectedFiles = new[]
        {
            Path.Combine(path, "file1.txt"),
            Path.Combine(path, "file2.txt"),
            Path.Combine(path, "file3.txt")
        };
        foreach (var filePath in expectedFiles)
        {
            await File.WriteAllTextAsync(filePath, "hello world");
        }

        // Act
        var watcher = new TextFileWatcher(_options, logger);
        var initialFiles = await watcher.InitialFiles.ToList().ToTask();

        // Assert
        initialFiles.ShouldBe(expectedFiles);
    }
    
    [Fact]
    public async Task ShouldObserveFileChanges()
    {
        // Arrange
        var path = GetTestDirectory();
        var logger = NullLogger<MarkdownFileWatcher>.Instance;
        var watcher = new MarkdownFileWatcher(_options, logger);
        var fileChangeEvent = watcher.Changed.Take(1).ToTask();
        
        // Trigger a file change event in the ~/Websites directory
        await File.WriteAllTextAsync(Path.Combine(path, "test.md"), "hello world");
        
        var result = await fileChangeEvent;
        result.FullPath.ShouldBe(Path.Combine(path, "test.md"));
    }

    private static string GetTestDirectory()
    {
        var tempDirectory = Path.GetTempPath();
        var testDirectory = Path.Combine(tempDirectory, "ReactiveFileWatcherTests");
        Directory.CreateDirectory(testDirectory);  // Ensure the directory exists
        return testDirectory;
    }
}