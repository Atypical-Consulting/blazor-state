namespace Reactif.Domain;

public sealed class FrontMatter
{
    private readonly IDictionary<string, object> _frontMatter;

    public FrontMatter(IDictionary<string, object> frontMatter)
    {
        _frontMatter = frontMatter;
    }
    
    public string Title
        => _frontMatter["title"] as string
           ?? string.Empty;
    
    public string Description
        => _frontMatter["description"] as string
           ?? string.Empty;
    
    public string Slug
        => _frontMatter["slug"] as string
           ?? string.Empty;

    public DateTime Date
        => DateTime.Parse(_frontMatter["date"] as string
                          ?? string.Empty);

    public string Author
        => _frontMatter["author"] as string
           ?? string.Empty;
    
    public string Layout
        => _frontMatter["layout"] as string
           ?? string.Empty;

    public List<string> Keywords
        => (_frontMatter["keywords"] as List<object> ?? new List<object>())
            .Select(x => x.ToString() ?? string.Empty)
            .ToList();

    public bool Draft
        => _frontMatter["draft"] as bool? ?? false;
}