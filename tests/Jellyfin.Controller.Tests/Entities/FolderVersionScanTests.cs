using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

[Collection("LibraryManagerTests")]
public class FolderVersionScanTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Scan_PreservesHiddenVersionsAndAllowsNewFiles(bool owned, bool exists)
    {
        var oldLibrary = BaseItem.LibraryManager;
        var oldSources = BaseItem.MediaSourceManager;
        var oldRepository = BaseItem.ItemRepository;
        try
        {
            var repository = new Mock<IItemRepository>();
            repository.Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(Array.Empty<BaseItem>());
            BaseItem.ItemRepository = repository.Object;
            var folder = new ScanFolder { Id = Guid.NewGuid(), Path = "/media/movies" };
            var primaryId = Guid.NewGuid();
            var persisted = new Movie
            {
                Id = Guid.NewGuid(), ParentId = folder.Id, Path = "/media/movies/movie.strm",
                PrimaryVersionId = primaryId, OwnerId = owned ? primaryId : Guid.Empty,
                PresentationUniqueKey = primaryId.ToString("N")
            };
            folder.Resolved = [new Movie { Id = persisted.Id, Path = persisted.Path }];
            var library = new Mock<ILibraryManager>();
            library.Setup(x => x.GetItemById(persisted.Id)).Returns(exists ? persisted : null);
            library.Setup(x => x.GetItemById(folder.Id)).Returns(folder);
            library.Setup(x => x.UpdateImagesAsync(It.IsAny<BaseItem>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
            BaseItem.LibraryManager = library.Object;
            var sources = new Mock<IMediaSourceManager>();
            sources.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
            BaseItem.MediaSourceManager = sources.Object;

            await folder.Scan(Mock.Of<IDirectoryService>());

            library.Verify(x => x.CreateItems(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<BaseItem>(), It.IsAny<CancellationToken>()), exists ? Times.Never() : Times.Once());
            Assert.Equal(primaryId, persisted.PrimaryVersionId);
            Assert.Equal(primaryId.ToString("N"), persisted.PresentationUniqueKey);
            Assert.Equal(owned ? primaryId : Guid.Empty, persisted.OwnerId);
        }
        finally
        {
            BaseItem.LibraryManager = oldLibrary;
            BaseItem.MediaSourceManager = oldSources;
            BaseItem.ItemRepository = oldRepository;
        }
    }

    private sealed class ScanFolder : Folder
    {
        public IReadOnlyList<BaseItem> Resolved { get; set; } = [];

        public override string GetClientTypeName() => "Folder";

        // General library queries hide alternate versions in Jellyfin 12.
        protected override IReadOnlyList<BaseItem> LoadChildren() => [];

        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService) => Resolved;

        public Task Scan(IDirectoryService directoryService) => ValidateChildrenInternal(
            new Progress<double>(),
            false,
            false,
            false,
            new MetadataRefreshOptions(directoryService),
            directoryService,
            CancellationToken.None);
    }
}
