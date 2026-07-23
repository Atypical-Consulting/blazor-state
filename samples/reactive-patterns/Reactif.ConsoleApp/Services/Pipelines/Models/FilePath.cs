namespace Reactif.ConsoleApp.Services.Pipelines.Models;

public record FilePath
{
    public FilePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("File path must be specified", nameof(value));
        
        Value = value;
    }

    public string Value { get; init; }
}