using Microsoft.Extensions.Time.Testing;
using WikiApi.Models;
using WikiApi.Services;

namespace WikiApi.Tests;

/// <summary>
/// Scaffolding smoke test so `dotnet test` always has at least one test to run before
/// the pipeline's unit-test-generator adds the bug/security/FIRST coverage. This test
/// exercises behaviour that is correct both before and after the seeded bugs are fixed,
/// so it stays green throughout the pipeline run.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Create_ThenGetById_ReturnsStoredDocument()
    {
        var store = new DocumentStore(new FakeTimeProvider());

        var created = store.Create(new CreateDocumentRequest("Sample", "Body", "me", null));
        var fetched = store.GetById(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Sample", fetched!.Title);
    }
}
