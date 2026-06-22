using WikiApi.Models;

namespace WikiApi.Services;

/// <summary>Storage and query operations for wiki documents.</summary>
public interface IDocumentStore
{
    IReadOnlyCollection<Document> GetAll();
    Document? GetById(Guid id);
    Document Create(CreateDocumentRequest request);
    Document? Update(Guid id, UpdateDocumentRequest request);
    bool Delete(Guid id);
    IReadOnlyCollection<Document> Search(string query);
}
