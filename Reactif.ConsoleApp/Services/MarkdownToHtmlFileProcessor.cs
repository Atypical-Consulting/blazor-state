using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using Reactif.ConsoleApp.Config;

namespace Reactif.ConsoleApp.Services;

public class MarkdownToHtmlFileProcessor : IMarkdownToHtmlFileProcessor
{
    private readonly IMarkdownProcessor _markdownProcessor;
    private readonly IFileService _fileService;
    private readonly ILogger<MarkdownToHtmlFileProcessor> _logger;
    private readonly string _outputDirectory;

    public MarkdownToHtmlFileProcessor(
        IMarkdownProcessor markdownProcessor,
        IFileService fileService,
        ILogger<MarkdownToHtmlFileProcessor> logger,
        IOptions<AppConfiguration> options)
    {
        _markdownProcessor = markdownProcessor ?? throw new ArgumentNullException(nameof(markdownProcessor));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outputDirectory = options?.Value.OutputDirectory ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_outputDirectory))
        {
            throw new ArgumentException("Output directory must be specified", nameof(options));
        }
    }

    public IObservable<Unit> ConvertMarkdownFileToHtml(string inputFilePath)
    {
        if (string.IsNullOrWhiteSpace(inputFilePath))
        {
            throw new ArgumentException("Input file path must be specified", nameof(inputFilePath));
        }
        
        return Observable.FromAsync(async () =>
        {
            var markdown = await _fileService.ReadAllTextAsync(inputFilePath);
            var (html, frontMatter) = _markdownProcessor.ConvertToHtml(markdown);
            var outputFileName = Path.ChangeExtension(Path.GetFileName(inputFilePath), ".html");
            var outputFilePath = Path.Combine(_outputDirectory, outputFileName);
            await _fileService.WriteAllTextAsync(outputFilePath, html);
            _logger.FileProcessed(inputFilePath, outputFilePath);
        });
    }
}

internal static partial class MarkdownToHtmlFileProcessorLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Processed {InputFilePath} -> {OutputFilePath}")]
    public static partial void FileProcessed(
        this ILogger<MarkdownToHtmlFileProcessor> logger,
        string inputFilePath, string outputFilePath);
}
