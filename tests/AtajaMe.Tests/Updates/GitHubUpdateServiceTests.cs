using AtajaMe.Domain.Updates;
using AtajaMe.Infrastructure.Updates;

namespace AtajaMe.Tests.Updates;

public sealed class GitHubUpdateServiceTests
{
    [Theory]
    [InlineData("v1.0.0", 1, 0, 0)]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("1.0.5", 1, 0, 5)]
    [InlineData("ver-2.0.1", 2, 0, 1)]
    [InlineData("release/3.1.0", 3, 1, 0)]
    public void ParseVersionFromTag_ValidTags_ReturnsParsedVersion(string tag, int major, int minor, int build)
    {
        var version = GitHubUpdateService.ParseVersionFromTag(tag);

        Assert.NotNull(version);
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(build, version.Build);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-tag")]
    [InlineData("beta")]
    public void ParseVersionFromTag_InvalidTags_ReturnsNull(string tag)
    {
        var version = GitHubUpdateService.ParseVersionFromTag(tag);
        Assert.Null(version);
    }

    [Fact]
    public void UpdateInfo_IsUpdateAvailable_ComparesSemVerAccurately()
    {
        var current = new Version(1, 0, 0);
        var newer = new Version(1, 0, 1);
        var older = new Version(0, 9, 9);

        var updateAvailable = new UpdateInfo(current, newer, "v1.0.1", "Notes", "http://dl", "http://page", newer > current);
        var updateNotAvailable = new UpdateInfo(current, older, "v0.9.9", "Notes", "http://dl", "http://page", older > current);

        Assert.True(updateAvailable.IsUpdateAvailable);
        Assert.False(updateNotAvailable.IsUpdateAvailable);
    }
}
