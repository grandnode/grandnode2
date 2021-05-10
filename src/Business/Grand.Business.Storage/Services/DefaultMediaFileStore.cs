//Source https://github.com/OrchardCMS/OrchardCore/tree/dev/src/OrchardCore

using Grand.Business.Storage.Interfaces;
using Microsoft.Extensions.FileProviders.Physical;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Grand.Business.Storage.Services
{
    public class DefaultMediaFileStore : IMediaFileStore
    {
        private readonly IFileStore _fileStore;

        public DefaultMediaFileStore(
            IFileStore fileStore)
        {
            _fileStore = fileStore;
        }


        public virtual Task<IFileStoreEntry> GetFileInfo(string path)
        {
            return _fileStore.GetFileInfo(path);
        }

        public virtual IFileStoreEntry GetDirectoryInfo(string path)
        {
            return _fileStore.GetDirectoryInfo(path);
        }
        public virtual Task<PhysicalDirectoryInfo> GetPhysicalDirectoryInfo(string directorypath)
        {
            return _fileStore.GetPhysicalDirectoryInfo(directorypath);
        }

        public virtual IList<IFileStoreEntry> GetDirectoryContent(string path = null, bool includeSubDirectories = false, bool listDirectories = true, bool listFiles = true)
        {
            return _fileStore.GetDirectoryContent(path, includeSubDirectories, listDirectories, listFiles);
        }

        public virtual bool TryCreateDirectory(string path)
        {
            return _fileStore.TryCreateDirectory(path);
        }

        public virtual Task<bool> TryRenameDirectory(string path, string newpath)
        {
            return _fileStore.TryRenameDirectory(path, newpath);
        }

        public virtual async Task<bool> TryDeleteFile(string path)
        {
            var result = await _fileStore.TryDeleteFile(path);

            return result;
        }

        public virtual async Task<bool> TryDeleteDirectory(string path)
        {
            var result = await _fileStore.TryDeleteDirectory(path);
            return result;
        }

        public virtual async Task MoveFile(string oldPath, string newPath)
        {
            await _fileStore.MoveFile(oldPath, newPath);
        }

        public virtual Task CopyFile(string srcPath, string dstPath)
        {
            return _fileStore.CopyFile(srcPath, dstPath);
        }
        public virtual Task RenameFile(string file, string newName)
        {
            return _fileStore.RenameFile(file, newName);
        }
        public virtual Task<Stream> GetFileStream(string path)
        {
            return _fileStore.GetFileStream(path);
        }

        public virtual Task<Stream> GetFileStream(IFileStoreEntry fileStoreEntry)
        {
            return _fileStore.GetFileStream(fileStoreEntry);
        }

        public virtual async Task<string> CreateFileFromStream(string path, Stream inputStream, bool overwrite = false)
        {
            return await _fileStore.CreateFileFromStream(path, inputStream, overwrite);
        }
        public virtual Task<string> ReadAllText(string path)
        {
            return _fileStore.ReadAllText(path);
        }
        public virtual Task WriteAllText(string path, string text)
        {
            return _fileStore.WriteAllText(path, text);
        }
    }
}
