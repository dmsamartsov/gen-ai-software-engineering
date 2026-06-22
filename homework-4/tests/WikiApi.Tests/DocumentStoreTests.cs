using Microsoft.Extensions.Time.Testing;
using WikiApi.Models;
using WikiApi.Services;

namespace WikiApi.Tests;

public class DocumentStoreTests
{
    [Theory]
    [InlineData("onboarding", "Onboarding Guide")]
    [InlineData("ONBOARDING", "Onboarding Guide")]
    [InlineData("OnBoArDiNg", "Onboarding Guide")]
    public void Search_TitleMatchIsCaseInsensitive_ReturnsMatchingDocument(string query, string expectedTitle)
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var store = new DocumentStore(timeProvider);

        // Act
        var results = store.Search(query);

        // Assert
        var match = Assert.Single(results);
        Assert.Equal(expectedTitle, match.Title);
    }

    [Theory]
    [InlineData("welcome", "Onboarding Guide")]
    [InlineData("WELCOME", "Onboarding Guide")]
    [InlineData("WeLcOmE", "Onboarding Guide")]
    public void Search_ContentMatchIsCaseInsensitive_ReturnsMatchingDocument(string query, string expectedTitle)
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var store = new DocumentStore(timeProvider);

        // Act
        var results = store.Search(query);

        // Assert
        var match = Assert.Single(results);
        Assert.Equal(expectedTitle, match.Title);
    }

    [Theory]
    [InlineData("hr", "Onboarding Guide")]
    [InlineData("HR", "Onboarding Guide")]
    [InlineData("hR", "Onboarding Guide")]
    public void Search_TagMatchIsCaseInsensitive_ReturnsMatchingDocument(string query, string expectedTitle)
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var store = new DocumentStore(timeProvider);

        // Act
        var results = store.Search(query);

        // Assert
        var match = Assert.Single(results);
        Assert.Equal(expectedTitle, match.Title);
    }

    [Fact]
    public void Update_ExistingDocument_PreservesOriginalCreatedAt()
    {
        // Arrange
        var initialTime = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.FromHours(3));
        var timeProvider = new FakeTimeProvider(initialTime);
        var store = new DocumentStore(timeProvider);

        var doc = store.Create(new CreateDocumentRequest("Test Title", "Content", "Author", null));
        var originalCreatedAt = doc.CreatedAt;

        // Advance time
        timeProvider.Advance(TimeSpan.FromHours(1));

        // Act
        var updated = store.Update(doc.Id, new UpdateDocumentRequest("New Title", "New Content", null));

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(originalCreatedAt, updated!.CreatedAt);
    }

    [Fact]
    public void Update_ExistingDocument_AdvancesUpdatedAtToNow()
    {
        // Arrange
        var initialTime = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.FromHours(3));
        var timeProvider = new FakeTimeProvider(initialTime);
        var store = new DocumentStore(timeProvider);

        var doc = store.Create(new CreateDocumentRequest("Test Title", "Content", "Author", null));
        
        // Advance time and check updated at matches the advanced time
        var advancedTime = initialTime.AddHours(2);
        timeProvider.SetUtcNow(advancedTime);

        // Act
        var updated = store.Update(doc.Id, new UpdateDocumentRequest("New Title", "New Content", null));

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(advancedTime, updated!.UpdatedAt);
    }

    [Fact]
    public void Update_NonExistentDocument_ReturnsNull()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var store = new DocumentStore(timeProvider);
        var nonExistentId = Guid.NewGuid();

        // Act
        var updated = store.Update(nonExistentId, new UpdateDocumentRequest("New Title", "New Content", null));

        // Assert
        Assert.Null(updated);
    }
}
