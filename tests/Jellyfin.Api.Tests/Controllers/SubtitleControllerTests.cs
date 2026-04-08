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
    public void IsLegacyPopSubAss_ReturnsTrueForPopSubMarkers()
    {
        const string subtitleText = """
[Script Info]
; // 此字幕由PopSub生成
Synch Point:0

[Events]
Dialogue: 0,0:00:01.00,0:00:02.00,*Default,NTP,0000,0000,0000,,你好
""";

        Assert.True(SubtitleController.IsLegacyPopSubAss(subtitleText));
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
    public void ApplyAndroidTvAssFontFallback_NormalizesPopSubStyleEncodingAndStyleReference()
    {
        const string subtitleText = """
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,方正准圆_GBK,45,&H00FFFFFF,&HF0000000,&H00EA1E0D,&HF0000000,-1,0,0,0,100,100,0,0.00,1,1,0,2,30,30,10,1

[Events]
Format: Layer, Start, End, Style, Actor, MarginL, MarginR, MarginV, Effect, Text
Dialogue: 0,0:00:02.93,0:00:03.35,*Default,NTP,0000,0000,0000,,大哥
""";

        var updatedText = SubtitleController.ApplyAndroidTvAssFontFallback(subtitleText);

        Assert.Contains("Style: Default,sans-serif,45", updatedText);
        Assert.Contains(",30,30,10,134", updatedText);
        Assert.Contains("Dialogue: 0,0:00:02.93,0:00:03.35,Default,NTP", updatedText);
        Assert.DoesNotContain("*Default", updatedText);
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
