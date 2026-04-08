using System;
using System.Text;
using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public class MediaInfoHelperTests
{
    [Fact]
    public void IsLegacyPopSubAss_ReturnsTrueForLegacyMarkers()
    {
        ReadOnlySpan<byte> subtitlePayload = Encoding.ASCII.GetBytes("""
[Script Info]
; // 此字幕由PopSub生成
Synch Point:0

[Events]
Dialogue: 0,0:00:01.00,0:00:02.00,*Default,NTP,0000,0000,0000,,你好
""");

        Assert.True(MediaInfoHelper.IsLegacyPopSubAss(subtitlePayload));
    }

    [Fact]
    public void IsLegacyPopSubAss_ReturnsFalseWithoutLegacyMarkers()
    {
        ReadOnlySpan<byte> subtitlePayload = Encoding.ASCII.GetBytes("""
[Script Info]
ScriptType: v4.00+

[Events]
Dialogue: 0,0:00:01.00,0:00:02.00,Default,NTP,0000,0000,0000,,你好
""");

        Assert.False(MediaInfoHelper.IsLegacyPopSubAss(subtitlePayload));
    }

    [Fact]
    public void BuildSubtitleDeliveryUrl_ReturnsExpectedApiUrl()
    {
        var itemId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var deliveryUrl = MediaInfoHelper.BuildSubtitleDeliveryUrl(
            itemId,
            "media-source-id",
            3,
            120000,
            "srt",
            "secret-token");

        Assert.Equal("/Videos/11111111-1111-1111-1111-111111111111/media-source-id/Subtitles/3/120000/Stream.srt?ApiKey=secret-token", deliveryUrl);
    }

    [Fact]
    public void AppendSubtitleConversionSuffix_AppendsSuffixOnce()
    {
        var title = MediaInfoHelper.AppendSubtitleConversionSuffix("Chinese", "[ASS->SRT]");
        var deduplicatedTitle = MediaInfoHelper.AppendSubtitleConversionSuffix(title, "[ASS->SRT]");

        Assert.Equal("Chinese [ASS->SRT]", title);
        Assert.Equal("Chinese [ASS->SRT]", deduplicatedTitle);
    }

    [Fact]
    public void AppendSubtitleConversionSuffix_UsesSuffixAsTitleWhenTitleMissing()
    {
        var title = MediaInfoHelper.AppendSubtitleConversionSuffix(null, "[ASS->SRT]");

        Assert.Equal("[ASS->SRT]", title);
    }
}
