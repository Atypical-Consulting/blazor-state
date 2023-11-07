namespace Reactif.Abstractions;

public interface IFileService
{
    /// <summary>
    /// Opens a text file, reads all lines of the file, and then closes the file.
    /// </summary>
    /// <param name="path">The file to open for reading.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>A task that represents the asynchronous read operation.</returns>
    Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Writes the specified string to a file asynchronously.
    /// </summary>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The string to write to the file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteAllTextAsync(
        string path,
        string contents,
        CancellationToken cancellationToken = default);
}