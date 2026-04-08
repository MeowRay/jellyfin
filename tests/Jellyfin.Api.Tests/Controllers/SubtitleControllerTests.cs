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
    public void ApplyAndroidTvAssFontFallback_RewritesAssStyleFontsToSansSerif()
    {
        const string subtitleText = """
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour
Style: Default,方正准圆_GBK,20,&H00FFFFFF
Style: Title,华康少女文字W5(P),20,&H00FFFFFF
""";

        var updatedText = SubtitleController.ApplyAndroidTvAssFontFallback(subtitleText);

        Assert.DoesNotContain("方正准圆_GBK", updatedText);
        Assert.DoesNotContain("华康少女文字W5(P)", updatedText);
        Assert.Equal(2, CountOccurrences(updatedText, ",sans-serif,"));
    }

    [Fact]
    public void ApplyAndroidTvAssFontFallback_RewritesInlineFnOverridesToSansSerif()
    {
        const string subtitleText = """
[Events]
Format: Layer, Start, End, Style, Actor, MarginL, MarginR, MarginV, Effect, Text
Dialogue: 0,0:00:01.00,0:00:02.00,Default,NTP,0000,0000,0000,,{\fn方正准圆_GBK}你好
""";

        var updatedText = SubtitleController.ApplyAndroidTvAssFontFallback(subtitleText);

        Assert.DoesNotContain(@"\fn方正准圆_GBK", updatedText);
        Assert.Contains(@"\fnsans-serif", updatedText);
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
