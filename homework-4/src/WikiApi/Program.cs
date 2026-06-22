using WikiApi.Auth;
using WikiApi.Models;
using WikiApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDocumentStore, DocumentStore>();
builder.Services.AddSingleton<AdminTokenValidator>();

var app = builder.Build();

var docs = app.MapGroup("/documents");

// List all documents.
docs.MapGet("/", (IDocumentStore store) => Results.Ok(store.GetAll()));

// Full-text search across documents.
docs.MapGet("/search", (string? q, IDocumentStore store) =>
    Results.Ok(store.Search(q ?? string.Empty)));

// Fetch a single document by id.
docs.MapGet("/{id:guid}", (Guid id, IDocumentStore store) =>
{
    var doc = store.GetById(id);
    return doc is null ? Results.NotFound() : Results.Ok(doc);
});

// Create a new document.
docs.MapPost("/", (CreateDocumentRequest request, IDocumentStore store) =>
{
    // NOTE: no input validation here — an empty Title or arbitrarily large Content is
    // accepted as-is (seeded weakness for the security review to flag).
    var created = store.Create(request);
    return Results.Created($"/documents/{created.Id}", created);
});

// Update an existing document.
docs.MapPut("/{id:guid}", (Guid id, UpdateDocumentRequest request, IDocumentStore store) =>
{
    var updated = store.Update(id, request);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

// Delete a document (privileged: requires a valid X-Admin-Token header).
docs.MapDelete("/{id:guid}", (Guid id, HttpRequest http, IDocumentStore store, AdminTokenValidator auth) =>
{
    var token = http.Headers["X-Admin-Token"].ToString();
    if (!auth.IsValid(token))
    {
        return Results.Unauthorized();
    }

    return store.Delete(id) ? Results.NoContent() : Results.NotFound();
});

app.Run();

// Exposed so WebApplicationFactory<Program> can host the app in integration tests.
public partial class Program { }
