using System.Collections.Concurrent;
using WikiApi.Models;

namespace WikiApi.Services;

/// <summary>
/// Thread-safe in-memory document store, seeded with a few sample wiki pages.
/// </summary>
public class DocumentStore : IDocumentStore
{
    private readonly ConcurrentDictionary<Guid, Document> _documents = new();
    private readonly TimeProvider _timeProvider;

    public DocumentStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        Seed();
    }

    public IReadOnlyCollection<Document> GetAll() =>
        _documents.Values.OrderBy(d => d.Title).ToList();

    public Document? GetById(Guid id) =>
        _documents.TryGetValue(id, out var doc) ? doc : null;

    public Document Create(CreateDocumentRequest request)
    {
        var now = _timeProvider.GetUtcNow();
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = Slugify(request.Title),
            Content = request.Content,
            Author = request.Author,
            CreatedAt = now,
            UpdatedAt = now,
            Tags = request.Tags ?? new List<string>()
        };
        _documents[doc.Id] = doc;
        return doc;
    }

    public Document? Update(Guid id, UpdateDocumentRequest request)
    {
        if (!_documents.TryGetValue(id, out var existing))
        {
            return null;
        }

        existing.Title = request.Title;
        existing.Content = request.Content;
        existing.Tags = request.Tags ?? existing.Tags;

        // BUG #2: an update stamps "now" onto CreatedAt (destroying the original
        // creation date) and never advances UpdatedAt. The two timestamps end up
        // inverted: CreatedAt moves forward on every edit while UpdatedAt is frozen.
        existing.CreatedAt = _timeProvider.GetUtcNow();

        return existing;
    }

    public bool Delete(Guid id) => _documents.TryRemove(id, out _);

    public IReadOnlyCollection<Document> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAll();
        }

        // BUG #1: Contains(string) uses an ordinal, case-sensitive comparison, so a
        // search for "vpn" never matches the "VPN Setup" page. Wiki search should be
        // case-insensitive across title, content, and tags.
        return _documents.Values
            .Where(d => d.Title.Contains(query)
                        || d.Content.Contains(query)
                        || d.Tags.Any(t => t.Contains(query)))
            .OrderBy(d => d.Title)
            .ToList();
    }

    private static string Slugify(string title) =>
        string.Join('-', title.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private void Seed()
    {
        var seedData = new[]
        {
            ("Onboarding Guide",
                "Welcome to the company. Covers your first week, accounts, and tooling.",
                "hr-team", new List<string> { "Onboarding", "HR" }),
            ("VPN Setup",
                "How to configure the corporate client on macOS, Windows, and Linux.",
                "it-team", new List<string> { "VPN", "Network", "IT" }),
            ("Expense Policy",
                "Reimbursement rules, approval limits, and how to submit a report.",
                "finance-team", new List<string> { "Finance", "Policy" })
        };

        foreach (var (title, content, author, tags) in seedData)
        {
            Create(new CreateDocumentRequest(title, content, author, tags));
        }
    }
}
