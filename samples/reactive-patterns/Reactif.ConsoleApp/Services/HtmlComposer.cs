namespace Reactif.ConsoleApp.Services;

public class HtmlComposer : IHtmlComposer
{
    private readonly ILogger<HtmlComposer> _logger;

    public HtmlComposer(ILogger<HtmlComposer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ComposeHtml(string content)
    {
        _logger.ComposingFinalHtml();

        return
            $"""
             <!DOCTYPE html>
             <html lang='en'>
             <head>
               <meta charset='UTF-8'>
               <meta name='viewport' content='width=device-width, initial-scale=1.0'>
               <script src="https://cdn.tailwindcss.com?plugins=forms,typography,aspect-ratio,line-clamp"></script>
               <title>Reactif</title>
             </head>
             <body>
               <div class='relative flex min-h-screen flex-col justify-center overflow-hidden bg-gray-50 py-6 sm:py-12'>
                 <div class='relative bg-white px-6 pt-10 pb-8 shadow-xl ring-1 ring-gray-900/5 sm:mx-auto sm:max-w-lg sm:rounded-lg sm:px-10'>
                   <div class='mx-auto max-w-md'>
                     <div class='divide-y divide-gray-300/50'>
                       <div class='space-y-6 py-8 text-base leading-7 text-gray-600'>
                         {content}
                       </div>
                     </div>
                   </div>
                 </div>
               </div>
             </body>
             </html>
             """;
    }
}

internal static partial class HtmlComposerLoggerExtensions
{
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Composing final HTML")]
    public static partial void ComposingFinalHtml(
        this ILogger<HtmlComposer> logger);
}