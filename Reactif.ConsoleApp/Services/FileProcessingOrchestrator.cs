using System.Reactive;
using System.Reactive.Linq;

namespace Reactif.ConsoleApp.Services;

/// <summary>
/// Orchestrates the processing of files by monitoring file changes and processing files accordingly.
/// </summary>
public class FileProcessingOrchestrator
{
    private readonly IFileWatcher _fileWatcher;
    private readonly IMarkdownToHtmlFileProcessor _markdownToHtmlFileProcessor;
    private readonly ILogger<FileProcessingOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessingOrchestrator"/> class.
    /// </summary>
    /// <param name="fileWatcher">The file watcher.</param>
    /// <param name="markdownToHtmlFileProcessor">The file processor.</param>
    /// <param name="logger">The logger.</param>
    public FileProcessingOrchestrator(
        IFileWatcher fileWatcher,
        IMarkdownToHtmlFileProcessor markdownToHtmlFileProcessor,
        ILogger<FileProcessingOrchestrator> logger)
    {
        _fileWatcher = 
            fileWatcher
            ?? throw new ArgumentNullException(nameof(fileWatcher));
        
        _markdownToHtmlFileProcessor =
            markdownToHtmlFileProcessor
            ?? throw new ArgumentNullException(nameof(markdownToHtmlFileProcessor));
        
        _logger = 
            logger 
            ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Begins processing files by monitoring for file changes and processing initial and changed files.
    /// </summary>
    /// <returns>A disposable object that can be used to unsubscribe from file events.</returns>
    public IDisposable ProcessFiles()
    {
        var initialFilesProcessing 
            = ProcessFiles(_fileWatcher.InitialFiles, "initial");
        var changedFilesProcessing 
            = ProcessFiles(_fileWatcher.Changed.Select(e => e.FullPath), "changed");

        return initialFilesProcessing
            .Merge(changedFilesProcessing)
            .Subscribe();
    }

    /// <summary>
    /// Processes the specified files.
    /// </summary>
    /// <param name="filePaths">An observable sequence of file paths.</param>
    /// <param name="fileProcessingType">A string indicating the type of file processing (e.g., "initial" or "changed").</param>
    /// <returns>An observable sequence that signals the completion of file processing.</returns>
    private IObservable<Unit> ProcessFiles(IObservable<string> filePaths, string fileProcessingType)
        => filePaths
            .SelectMany(filePath => _markdownToHtmlFileProcessor.ConvertMarkdownFileToHtml(filePath))
            .Catch<Unit, Exception>(ex =>
            {
                _logger.ProcessFilesException(fileProcessingType);
                return Observable.Empty<Unit>();
            });
}

internal static partial class FileProcessingOrchestratorLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "An error occurred while processing {FileProcessingType} files")]
    public static partial void ProcessFilesException(
        this ILogger<FileProcessingOrchestrator> logger, string fileProcessingType);
}