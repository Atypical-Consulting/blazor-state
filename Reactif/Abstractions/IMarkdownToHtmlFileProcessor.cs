using System.Reactive;

namespace Reactif.Abstractions;

public interface IMarkdownToHtmlFileProcessor
{
    IObservable<Unit> ConvertMarkdownFileToHtml(string inputFilePath);
}