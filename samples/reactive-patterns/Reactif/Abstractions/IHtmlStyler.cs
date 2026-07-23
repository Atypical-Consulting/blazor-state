namespace Reactif.Abstractions;

/// <summary>
/// Defines the functionality for styling HTML content.
/// </summary>
public interface IHtmlStyler
{
    /// <summary>
    /// Applies styles to HTML content based on predefined CSS classes.
    /// </summary>
    /// <param name="htmlContent">The HTML content to be styled.</param>
    /// <returns>The styled HTML content.</returns>
    string StyleHtmlContent(string htmlContent);
}
