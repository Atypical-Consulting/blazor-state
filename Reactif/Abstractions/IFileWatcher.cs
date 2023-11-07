namespace Reactif.Abstractions;

public interface IFileWatcher
{
    /// <summary>
    /// Gets an observable sequence of file paths initially present in the watched directory.
    /// </summary>
    IObservable<string> InitialFiles { get; }

    /// <summary>
    /// Gets an observable sequence of FileSystemEventArgs representing file creation events.
    /// </summary>
    IObservable<FileSystemEventArgs> Changed { get; }
}