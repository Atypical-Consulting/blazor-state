namespace Reactif.Abstractions;

/// <summary>
/// Defines the functionality for composing HTML content into a complete HTML document.
/// </summary>
public interface IHtmlComposer
{
    /// <summary>
    /// Composes the provided HTML content into a full HTML document.
    /// </summary>
    /// <param name="content">The HTML content to be wrapped in an HTML document structure.</param>
    /// <returns>A string containing the full HTML document.</returns>
    string ComposeHtml(string content);
}
