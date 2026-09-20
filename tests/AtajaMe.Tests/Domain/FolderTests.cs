using AtajaMe.Domain.Entities;
using Xunit;

namespace AtajaMe.Tests.Domain;

public class FolderTests
{
    [Fact]
    public void Create_WithValidName_ShouldInstantiateFolder()
    {
        // Act
        var folder = Folder.Create("Plantillas de Correo");

        // Assert
        Assert.NotEqual(Guid.Empty, folder.Id);
        Assert.Equal("Plantillas de Correo", folder.Name);
        Assert.Null(folder.ParentId);
        Assert.Equal(0, folder.SortOrder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Assert
        Assert.Throws<ArgumentException>(() => Folder.Create(invalidName));
    }

    [Fact]
    public void MoveTo_WithSameId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var folder = Folder.Create("Carpeta A");

        // Assert
        Assert.Throws<InvalidOperationException>(() => folder.MoveTo(folder.Id));
    }

    [Fact]
    public void UpdateName_ShouldChangeNameAndTouchUpdatedAt()
    {
        // Arrange
        var folder = Folder.Create("Antiguo");
        var originalUpdated = folder.UpdatedAt;

        // Act
        folder.UpdateName("Nuevo");

        // Assert
        Assert.Equal("Nuevo", folder.Name);
        Assert.True(folder.UpdatedAt >= originalUpdated);
    }
}
