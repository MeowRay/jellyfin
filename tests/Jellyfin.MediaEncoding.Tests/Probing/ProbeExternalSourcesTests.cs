using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Probing
{
    public class ProbeExternalSourcesTests
    {
        [Fact]
        public void GetExtraArguments_Forwards_UserAgent()
        {
            var encoder = new MediaEncoder(
                Mock.Of<ILogger<MediaEncoder>>(),
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<IFileSystem>(),
                Mock.Of<IBlurayExaminer>(),
                Mock.Of<ILocalizationManager>(),
                new ConfigurationBuilder().Build(),
                Mock.Of<IServerConfigurationManager>());

            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            var req = new MediaBrowser.Controller.MediaEncoding.MediaInfoRequest()
            {
                MediaSource = new MediaBrowser.Model.Dto.MediaSourceInfo
                {
                    Path = "/path/to/stream",
                    Protocol = MediaProtocol.Http,
                    RequiredHttpHeaders = new Dictionary<string, string>()
                    {
                        { "User-Agent", userAgent },
                    }
                },
                ExtractChapters = false,
                MediaType = MediaBrowser.Model.Dlna.DlnaProfileType.Video,
            };

            var extraArg = encoder.GetExtraArguments(req);

            Assert.Contains($"-user_agent \"{userAgent}\"", extraArg, StringComparison.InvariantCulture);
        }

        [Fact]
        public void GetFastProbeArguments_UsesConfiguredLimitsForMatchingHttpUrl()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>()
                {
                    { "FFmpeg:fastProbeSize", "32M" },
                    { "FFmpeg:fastAnalyzeDuration", "30M" },
                    { "FFmpeg:fastProbeUrlPrefixes", "http://streamlinker:3008/stream/" },
                })
                .Build();
            var encoder = CreateEncoder(config);
            var request = CreateVideoRequest("http://streamlinker:3008/stream/123");

            var arguments = encoder.GetFastProbeArguments(request);

            Assert.Contains("-analyzeduration 30M", arguments, StringComparison.Ordinal);
            Assert.Contains("-probesize 32M", arguments, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("file:///media/video.mkv", MediaProtocol.File)]
        [InlineData("http://another-server/video.mkv", MediaProtocol.Http)]
        public void GetFastProbeArguments_IgnoresNonMatchingSources(string path, MediaProtocol protocol)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>()
                {
                    { "FFmpeg:fastProbeSize", "32M" },
                    { "FFmpeg:fastAnalyzeDuration", "30M" },
                    { "FFmpeg:fastProbeUrlPrefixes", "http://streamlinker:3008/stream/" },
                })
                .Build();
            var encoder = CreateEncoder(config);
            var request = CreateVideoRequest(path);
            request.MediaSource.Protocol = protocol;

            Assert.Null(encoder.GetFastProbeArguments(request));
        }

        [Fact]
        public void IsProbeResultComplete_RequiresDurationVideoAndAudio()
        {
            var mediaInfo = new MediaInfo
            {
                RunTimeTicks = TimeSpan.FromMinutes(24).Ticks,
                MediaStreams =
                [
                    new MediaStream { Type = MediaStreamType.Video },
                    new MediaStream { Type = MediaStreamType.Audio },
                ]
            };

            Assert.True(MediaEncoder.IsProbeResultComplete(mediaInfo, MediaBrowser.Model.Dlna.DlnaProfileType.Video));

            mediaInfo.MediaStreams = [new MediaStream { Type = MediaStreamType.Video }];
            Assert.False(MediaEncoder.IsProbeResultComplete(mediaInfo, MediaBrowser.Model.Dlna.DlnaProfileType.Video));

            mediaInfo.MediaStreams = [new MediaStream { Type = MediaStreamType.Audio }];
            Assert.True(MediaEncoder.IsProbeResultComplete(mediaInfo, MediaBrowser.Model.Dlna.DlnaProfileType.Audio));

            mediaInfo.RunTimeTicks = null;
            Assert.False(MediaEncoder.IsProbeResultComplete(mediaInfo, MediaBrowser.Model.Dlna.DlnaProfileType.Audio));
        }

        private static MediaEncoder CreateEncoder(IConfiguration configuration)
            => new MediaEncoder(
                Mock.Of<ILogger<MediaEncoder>>(),
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<IFileSystem>(),
                Mock.Of<IBlurayExaminer>(),
                Mock.Of<ILocalizationManager>(),
                configuration,
                Mock.Of<IServerConfigurationManager>());

        private static MediaBrowser.Controller.MediaEncoding.MediaInfoRequest CreateVideoRequest(string path)
            => new MediaBrowser.Controller.MediaEncoding.MediaInfoRequest()
            {
                MediaSource = new MediaBrowser.Model.Dto.MediaSourceInfo
                {
                    Path = path,
                    Protocol = MediaProtocol.Http,
                },
                ExtractChapters = false,
                MediaType = MediaBrowser.Model.Dlna.DlnaProfileType.Video,
            };
    }
}
