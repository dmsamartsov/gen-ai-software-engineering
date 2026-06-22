namespace WikiApi.Models;

/// <summary>Payload for creating a new wiki document.</summary>
public record CreateDocumentRequest(string Title, string Content, string Author, List<string>? Tags);

/// <summary>Payload for updating an existing wiki document.</summary>
public record UpdateDocumentRequest(string Title, string Content, List<string>? Tags);
