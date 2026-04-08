using Jellyfin.Api.Controllers;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class SubtitleControllerTests
{
    [Theory]
    [InlineData("Jellyfin Android TV", null, null, true)]
    [InlineData(null, "Android TV", null, true)]
    [InlineData(null, null, "Jellyfin Android TV", true)]
    [InlineData("Jellyfin Mobile", null, null, false)]
    [InlineData(null, "Pixel 9", "okhttp/4.12.0", false)]
    public void IsAndroidTvSubtitleRequest_ReturnsExpectedResult(
        string? client,
        string? device,
        string? userAgent,
        bool expected)
    {
        Assert.Equal(expected, SubtitleController.IsAndroidTvSubtitleRequest(client, device, userAgent));
    }

    [Fact]
    public void ApplyAndroidTvAssFontFallback_RewritesArialUnicodeMsToSansSerif()
    {
        const string subtitleText = """
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour
Style: Default,Arial Unicode MS,20,&H00FFFFFF
""";

        var updatedText = SubtitleController.ApplyAndroidTvAssFontFallback(subtitleText);

        Assert.DoesNotContain("Arial Unicode MS", updatedText);
        Assert.Contains(",sans-serif,", updatedText);
    }
}
