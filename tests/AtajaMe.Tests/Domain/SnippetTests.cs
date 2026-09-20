using AtajaMe.Domain.Entities;
using Xunit;

namespace AtajaMe.Tests.Domain;

public class SnippetTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInstantiateSnippetCorrectly()
    {
        // Arrange & Act
        var snippet = Snippet.Create("bd", "Buenos Días", "¡Hola, muy buenos días!");

        // Assert
        Assert.NotEqual(Guid.Empty, snippet.Id);
        Assert.Equal("bd", snippet.Code);
        Assert.Equal("Buenos Días", snippet.Name);
        Assert.Equal("¡Hola, muy buenos días!", snippet.Content);
        Assert.True(snippet.IsActive);
        Assert.False(snippet.IsFavorite);
        Assert.False(snippet.IsInTrash);
        Assert.Null(snippet.DeletedAt);
        Assert.Equal(0, snippet.UsageCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNullCode_ShouldThrowArgumentException(string? invalidCode)
    {
        // Assert
        Assert.Throws<ArgumentException>(() =>
            Snippet.Create(invalidCode!, "Nombre", "Contenido"));
    }

    [Fact]
    public void MoveToTrash_ShouldMarkAsInTrashAndSetDeletedAt()
    {
        // Arrange
        var snippet = Snippet.Create("bd", "Buenos Días", "¡Hola!");

        // Act
        snippet.MoveToTrash();

        // Assert
        Assert.True(snippet.IsInTrash);
        Assert.NotNull(snippet.DeletedAt);
    }

    [Fact]
    public void RestoreFromTrash_ShouldUnmarkInTrashAndClearDeletedAt()
    {
        // Arrange
        var snippet = Snippet.Create("bd", "Buenos Días", "¡Hola!");
        snippet.MoveToTrash();

        // Act
        snippet.RestoreFromTrash();

        // Assert
        Assert.False(snippet.IsInTrash);
        Assert.Null(snippet.DeletedAt);
    }

    [Fact]
    public void RecordUsage_ShouldIncrementUsageCountAndSetLastUsedAt()
    {
        // Arrange
        var snippet = Snippet.Create("sig", "Firma", "Saludos cordiales.");

        // Act
        snippet.RecordUsage();

        // Assert
        Assert.Equal(1, snippet.UsageCount);
        Assert.NotNull(snippet.LastUsedAt);
    }

    [Fact]
    public void Duplicate_ShouldCreateNewInstanceWithNewCodeAndSuffix()
    {
        // Arrange
        var original = Snippet.Create("sig1", "Firma", "Saludos cordiales.");

        // Act
        var copy = original.Duplicate("sig2");

        // Assert
        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal("sig2", copy.Code);
        Assert.Equal("Firma (Copia)", copy.Name);
        Assert.Equal(original.Content, copy.Content);
        Assert.False(copy.IsFavorite);
        Assert.False(copy.IsInTrash);
    }

    [Theory]
    [InlineData("BD", "bd", false, true)]
    [InlineData("BD", "bd", true, false)]
    [InlineData("bd", "bd", true, true)]
    public void MatchesCode_ShouldRespectCaseSensitivity(string snippetCode, string inputCode, bool caseSensitive, bool expectedMatch)
    {
        // Arrange
        var snippet = Snippet.Create(snippetCode, "Prueba", "Contenido", caseSensitive: caseSensitive);

        // Act
        var match = snippet.MatchesCode(inputCode);

        // Assert
        Assert.Equal(expectedMatch, match);
    }
}
