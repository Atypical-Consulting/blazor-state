using Reactif.Domain;

namespace Reactif.Abstractions;

public interface IMarkdownToHtmlConverter
{
    (string html, FrontMatter frontMatter) Convert(string markdown);
}