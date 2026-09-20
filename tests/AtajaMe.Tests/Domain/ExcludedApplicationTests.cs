using AtajaMe.Domain.Entities;
using Xunit;

namespace AtajaMe.Tests.Domain;

public class ExcludedApplicationTests
{
    [Fact]
    public void Create_WithValidProcessName_ShouldInstantiateAndNormalizeName()
    {
        // Act
        var app = ExcludedApplication.Create("notepad.exe", "Editor de notas de Windows");

        // Assert
        Assert.NotEqual(Guid.Empty, app.Id);
        Assert.Equal("notepad", app.ProcessName);
        Assert.Equal("Editor de notas de Windows", app.Description);
        Assert.True(app.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNullProcessName_ShouldThrowArgumentException(string? invalidProcessName)
    {
        // Assert
        Assert.Throws<ArgumentException>(() => ExcludedApplication.Create(invalidProcessName!));
    }

    [Theory]
    [InlineData("code", "code.exe", true)]
    [InlineData("code.exe", "CODE", true)]
    [InlineData("devenv", "devenv.EXE", true)]
    [InlineData("notepad", "calc", false)]
    [InlineData("notepad", "", false)]
    [InlineData("notepad", null, false)]
    public void Matches_ShouldCompareCorrectlyCaseInsensitiveAndIgnoringExeExtension(
        string registeredName,
        string? targetName,
        bool expectedMatch)
    {
        // Arrange
        var app = ExcludedApplication.Create(registeredName);

        // Act
        var result = app.Matches(targetName!);

        // Assert
        Assert.Equal(expectedMatch, result);
    }
}
