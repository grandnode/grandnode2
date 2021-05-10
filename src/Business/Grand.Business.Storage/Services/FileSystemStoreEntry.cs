//Source https://github.com/OrchardCMS/OrchardCore/tree/dev/src/OrchardCore
using Grand.Business.Storage.Interfaces;
using Microsoft.Extensions.FileProviders;
using System;

namespace Grand.Business.Storage.Services
{
    public class FileSystemStoreEntry : IFileStoreEntry
    {
        private readonly IFileInfo _fileInfo;
        private readonly string _path;

        internal FileSystemStoreEntry(string path, IFileInfo fileInfo)
        {
            _fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string Path => _path;
        public string Name => _fileInfo.Name;
        public string DirectoryPath => _path.Substring(0, _path.Length - Name.Length).TrimEnd('/');
        public string PhysicalPath => _fileInfo.PhysicalPath;
        public DateTime LastModifiedUtc => _fileInfo.LastModified.UtcDateTime;
        public long Length => _fileInfo.Length;
        public bool IsDirectory => _fileInfo.IsDirectory;
    }
}
