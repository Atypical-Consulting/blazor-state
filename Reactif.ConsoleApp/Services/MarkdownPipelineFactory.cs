using Markdig;

namespace Reactif.ConsoleApp.Services;

public class MarkdownPipelineFactory : IMarkdownPipelineFactory
{
    private readonly ILogger<MarkdownPipelineFactory> _logger;

    public MarkdownPipelineFactory(ILogger<MarkdownPipelineFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MarkdownPipeline CreatePipeline()
    {
        _logger.CreatingMarkdownPipeline();
        
        return new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .Build();
    }
}

internal static partial class MarkdownPipelineFactoryLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Creating Markdown pipeline")]
    public static partial void CreatingMarkdownPipeline(
        this ILogger<MarkdownPipelineFactory> logger);
}
