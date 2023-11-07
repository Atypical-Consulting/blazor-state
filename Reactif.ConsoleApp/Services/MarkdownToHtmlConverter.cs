using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Reactif.Domain;

namespace Reactif.ConsoleApp.Services;

/// <summary>
/// Represents a markdown processor.
/// </summary>
public class MarkdownToHtmlConverter : IMarkdownToHtmlConverter
{
    private readonly ILogger<MarkdownToHtmlConverter> _logger;
    private readonly IMarkdownPipelineFactory _pipelineFactory;
    private readonly IHtmlStyler _htmlStyler;
    private readonly IHtmlComposer _htmlComposer;

    /// <summary>
    /// Creates a new instance of <see cref="MarkdownToHtmlConverter"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pipelineFactory">The pipeline factory.</param>
    /// <param name="htmlStyler">The HTML styler.</param>
    /// <param name="htmlComposer">The HTML composer.</param>
    public MarkdownToHtmlConverter(
        ILogger<MarkdownToHtmlConverter> logger,
        IMarkdownPipelineFactory pipelineFactory,
        IHtmlStyler htmlStyler,
        IHtmlComposer htmlComposer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
        _htmlStyler = htmlStyler ?? throw new ArgumentNullException(nameof(htmlStyler));
        _htmlComposer = htmlComposer ?? throw new ArgumentNullException(nameof(htmlComposer));
    }

    /// <summary>
    /// Converts the specified markdown to HTML.
    /// </summary>
    /// <param name="markdown">The markdown to convert.</param>
    /// <returns>The HTML and the front matter.</returns>
    public (string html, FrontMatter frontMatter) Convert(string markdown)
    {
        _logger.ConvertingMarkdownToHTML();
        var pipeline = _pipelineFactory.CreatePipeline();
       
        var markdownDocument = Markdown.Parse(markdown, pipeline);
        var htmlContent = markdownDocument.ToHtml(pipeline);

        var styledHtmlContent = _htmlStyler.StyleHtmlContent(htmlContent);
        var finalHtml = _htmlComposer.ComposeHtml(styledHtmlContent);

        var frontMatterBlock = markdownDocument
            .Descendants<YamlFrontMatterBlock>()
            .FirstOrDefault();
        
        var frontMatterDictionary = frontMatterBlock != null
            ? new YamlDotNet.Serialization.Deserializer()
                .Deserialize<IDictionary<string, object>>(frontMatterBlock.Lines.ToString())
            : new Dictionary<string, object>();
        
        var frontMatter = new FrontMatter(frontMatterDictionary);

        return (finalHtml, frontMatter);
    }
}

internal static partial class MarkdownProcessorLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Converting markdown to HTML")]
    public static partial void ConvertingMarkdownToHTML(
        this ILogger<MarkdownToHtmlConverter> logger);
}
