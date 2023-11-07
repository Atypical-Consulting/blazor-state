using Reactif.Domain;

namespace Reactif.Abstractions;

public interface IMarkdownProcessor
{
    (string html, FrontMatter frontMatter) ConvertToHtml(string markdown);
}