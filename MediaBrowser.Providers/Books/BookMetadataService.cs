#pragma warning disable CS1591

using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Books
{
    public class BookMetadataService : MetadataService<Book, BookInfo>
    {
        public BookMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<BookMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            IPathManager pathManager,
            IKeyframeManager keyframeManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager, pathManager, keyframeManager)
        {
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Book> source, MetadataResult<Book> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            if (replaceData || string.IsNullOrEmpty(target.Item.SeriesName))
            {
                target.Item.SeriesName = source.Item.SeriesName;
            }
        }
    }
}
