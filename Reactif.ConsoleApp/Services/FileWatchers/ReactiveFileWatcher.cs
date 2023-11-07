using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Options;
using Reactif.ConsoleApp.Config;

namespace Reactif.ConsoleApp.Services.FileWatchers;

/// <summary>
/// Represents a file system watcher that raises events when a file is created, changed, or deleted.
/// </summary>
public abstract class ReactiveFileWatcher : IFileWatcher, IDisposable
{
    private readonly FileSystemWatcher _fileSystemWatcher;
    private readonly ILogger<ReactiveFileWatcher> _logger;
    
    private readonly ReplaySubject<string> _initialFilesSubject = new();
    private readonly Subject<FileSystemEventArgs> _changedSubject = new();

    /// <summary>
    /// Gets the file filter.
    /// </summary>
    protected abstract string FileFilter { get; }
    
    /// <summary>
    /// Gets an observable sequence of file paths initially present in the watched directory.
    /// </summary>
    public IObservable<string> InitialFiles
        => _initialFilesSubject.AsObservable();

    /// <summary>
    /// Gets an observable sequence of FileSystemEventArgs representing file creation events.
    /// </summary>
    public IObservable<FileSystemEventArgs> Changed
        => _changedSubject.AsObservable();

    /// <summary>
    /// Creates a new instance of <see cref="ReactiveFileWatcher"/>.
    /// </summary>
    /// <param name="options">The application configuration.</param>
    /// <param name="logger">The logger.</param>
    public ReactiveFileWatcher(
        IOptions<AppConfiguration> options,
        ILogger<ReactiveFileWatcher> logger)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        
        var inputDirectory = options.Value.InputDirectory;
        
        _fileSystemWatcher = new FileSystemWatcher(inputDirectory)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            Filter = FileFilter
        };
        
        _logger = logger;
        _fileSystemWatcher.Changed += OnFileChanged;
        PopulateInitialFiles();
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _fileSystemWatcher?.Dispose();
        _initialFilesSubject?.Dispose();
        _changedSubject?.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ReactiveFileWatcher()
    {
        Dispose(false);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _changedSubject.OnNext(e);
    }

    private void PopulateInitialFiles()
    {
        try
        {
            var searchPattern = _fileSystemWatcher.Filter;
            var searchOption = _fileSystemWatcher.IncludeSubdirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var files = Directory.GetFiles(_fileSystemWatcher.Path, searchPattern, searchOption);

            foreach (var file in files)
            {
                _initialFilesSubject.OnNext(file);
            }

            _initialFilesSubject.OnCompleted();
        }
        catch (Exception ex)
        {
            _logger.PopulateInitialFilesException();
            _initialFilesSubject.OnError(ex);
        }
    }
}

internal static partial class ReactiveFileWatcherLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "An error occurred while getting initial files")]
    public static partial void PopulateInitialFilesException(
        this ILogger<ReactiveFileWatcher> logger);
}