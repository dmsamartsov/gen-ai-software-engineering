namespace WikiApi.Models;

/// <summary>
/// A single page in the internal wiki.
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}
