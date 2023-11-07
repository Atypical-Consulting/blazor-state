using HtmlAgilityPack;

namespace Reactif.ConsoleApp.Services;

public class HtmlStyler : IHtmlStyler
{
    private static readonly Dictionary<string, string> ClassMap = new()
    {
        { "h1", "text-3xl" },
        { "h2", "text-2xl" },
        { "h3", "text-xl" },
        { "h4", "text-lg" },
        { "h5", "text-base" },
        { "h6", "text-sm" },
        { "p", "mb-4" },
        { "a", "text-sky-500 hover:text-sky-600" },
        { "blockquote", "border-l-4 border-gray-300/50 pl-4" },
        { "table", "border-collapse border border-gray-300/50" },
        { "th", "border border-gray-300/50 px-4 py-2" },
        { "td", "border border-gray-300/50 px-4 py-2" },
        { "thead", "bg-gray-100" },
        { "tbody", "bg-white divide-y divide-gray-300/50" },
        { "tr", "bg-white" },
        { "ul", "list-disc list-inside" },
        { "ol", "list-decimal list-inside" },
        { "hr", "border-gray-300/50 my-8" },
        { "pre", "bg-gray-100 rounded-lg p-4" },
        { "code", "text-gray-900" }
    };
    
    private readonly ILogger<HtmlStyler> _logger;

    public HtmlStyler(ILogger<HtmlStyler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string StyleHtmlContent(string htmlContent)
    {
        _logger.StylingHtmlContent();
        
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        foreach (var (key, value) in ClassMap)
        {
            AddClassToNodes(doc, $"//{key}", value);
        }

        return doc.DocumentNode.OuterHtml;
    }

    private static void AddClassToNodes(HtmlDocument doc, string xpath, string className)
    {
        foreach (var node in doc.DocumentNode.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>())
        {
            node.AddClass(className);
        }
    }
}

internal static partial class HtmlStylerLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Styling HTML content")]
    public static partial void StylingHtmlContent(
        this ILogger<HtmlStyler> logger);
}