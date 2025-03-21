#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using BitFaster.Caching;
using BitFaster.Caching.Lru;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        private static ICache<string, FileSystemMetadata[]> _cache = null!;
        private static ICache<string, FileSystemMetadata> _fileCache = null!;
        private static ICache<string, List<string>> _filePathCache = null!;

        private readonly IFileSystem _fileSystem;

        public DirectoryService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public static void Initialize(IServerConfigurationManager configurationManager)
        {
            ArgumentNullException.ThrowIfNull(configurationManager);

            _cache = new ConcurrentLruBuilder<string, FileSystemMetadata[]>()
                .WithCapacity(configurationManager.Configuration.LibraryCacheSize)
                .WithExpireAfterAccess(TimeSpan.FromMinutes(10))
                .WithKeyComparer(StringComparer.Ordinal)
                .Build();

            _fileCache = new ConcurrentLruBuilder<string, FileSystemMetadata>()
                .WithCapacity(configurationManager.Configuration.LibraryCacheSize)
                .WithExpireAfterAccess(TimeSpan.FromMinutes(10))
                .WithKeyComparer(StringComparer.Ordinal)
                .Build();

            _filePathCache = new ConcurrentLruBuilder<string, List<string>>()
                .WithCapacity(configurationManager.Configuration.LibraryCacheSize)
                .WithExpireAfterAccess(TimeSpan.FromMinutes(10))
                .WithKeyComparer(StringComparer.Ordinal)
                .Build();
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            return _cache.GetOrAdd(path, static (p, fileSystem) => fileSystem.GetFileSystemEntries(p).ToArray(), _fileSystem);
        }

        public List<FileSystemMetadata> GetDirectories(string path)
        {
            var list = new List<FileSystemMetadata>();
            var items = GetFileSystemEntries(path);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.IsDirectory)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public List<FileSystemMetadata> GetFiles(string path)
        {
            var list = new List<FileSystemMetadata>();
            var items = GetFileSystemEntries(path);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (!item.IsDirectory)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public FileSystemMetadata? GetFile(string path)
        {
            var entry = GetFileSystemEntry(path);
            return entry is not null && !entry.IsDirectory ? entry : null;
        }

        public FileSystemMetadata? GetDirectory(string path)
        {
            var entry = GetFileSystemEntry(path);
            return entry is not null && entry.IsDirectory ? entry : null;
        }

        public FileSystemMetadata? GetFileSystemEntry(string path)
        {
            if (!_fileCache.TryGet(path, out var result))
            {
                var file = _fileSystem.GetFileSystemInfo(path);
                if (file?.Exists ?? false)
                {
                    result = file;
                    _fileCache.AddOrUpdate(path, result);
                }
            }

            return result;
        }

        public IReadOnlyList<string> GetFilePaths(string path)
            => GetFilePaths(path, false);

        public IReadOnlyList<string> GetFilePaths(string path, bool clearCache, bool sort = false)
        {
            if (clearCache)
            {
                _filePathCache.TryRemove(path, out _);
            }

            var filePaths = _filePathCache.GetOrAdd(path, static (p, fileSystem) => fileSystem.GetFilePaths(p).ToList(), _fileSystem);

            if (sort)
            {
                filePaths.Sort();
            }

            return filePaths;
        }

        public bool IsAccessible(string path)
        {
            return _fileSystem.GetFileSystemEntryPaths(path).Any();
        }
    }
}
