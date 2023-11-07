namespace Reactif.ConsoleApp.Services;

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        _logger.ReadingFile(path);
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task WriteAllTextAsync(
        string path,
        string contents,
        CancellationToken cancellationToken = default)
    {
        _logger.WritingFile(path, contents.Length);
        await File.WriteAllTextAsync(path, contents, cancellationToken);
    }
}

internal static partial class FileServiceLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Debug,
        Message = "Reading file at `{Path}`")]
    public static partial void ReadingFile(
        this ILogger<FileService> logger, string path);
    
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Writing file at `{Path}` with a length of `{Length}`")]
    public static partial void WritingFile(
        this ILogger<FileService> logger, string path, int length);
}
