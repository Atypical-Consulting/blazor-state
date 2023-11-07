using Markdig;

namespace Reactif.Abstractions;

/// <summary>
/// Defines a factory for creating instances of a MarkdownPipeline.
/// </summary>
public interface IMarkdownPipelineFactory
{
    /// <summary>
    /// Creates and configures an instance of a MarkdownPipeline.
    /// </summary>
    /// <returns>A configured MarkdownPipeline instance.</returns>
    MarkdownPipeline CreatePipeline();
}
