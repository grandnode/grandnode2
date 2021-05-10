//Source https://github.com/OrchardCMS/OrchardCore/tree/dev/src/OrchardCore

using Grand.Business.Storage.Interfaces;
using Microsoft.Extensions.FileProviders.Physical;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Storage.Services
{
    public class FileSystemStore : IFileStore
    {
        private readonly string _fileSystemPath;

        public FileSystemStore(string fileSystemPath)
        {
            _fileSystemPath = Path.GetFullPath(fileSystemPath);
        }

        public Task<IFileStoreEntry> GetFileInfo(string path)
        {
            var physicalPath = GetPhysicalPath(path);

            var fileInfo = new PhysicalFileInfo(new FileInfo(physicalPath));

            if (fileInfo.Exists)
            {
                return Task.FromResult<IFileStoreEntry>(new FileSystemStoreEntry(path, fileInfo));
            }

            return Task.FromResult<IFileStoreEntry>(null);
        }

        public IFileStoreEntry GetDirectoryInfo(string path)
        {
            var physicalPath = GetPhysicalPath(path);

            var directoryInfo = new PhysicalDirectoryInfo(new DirectoryInfo(physicalPath));

            if (directoryInfo.Exists)
            {
                return new FileSystemStoreEntry(path, directoryInfo);
            }

            return null;
        }

        public Task<PhysicalDirectoryInfo> GetPhysicalDirectoryInfo(string directorypath)
        {
            var physicalPath = GetPhysicalPath(directorypath);

            var directoryInfo = new PhysicalDirectoryInfo(new DirectoryInfo(physicalPath));

            if (directoryInfo.Exists)
            {
                return Task.FromResult(directoryInfo);
            }

            return Task.FromResult<PhysicalDirectoryInfo>(null);
        }
        public IList<IFileStoreEntry> GetDirectoryContent(string path = null, bool includeSubDirectories = false, bool listDirectories = true, bool listFiles = true)
        {
            var physicalPath = GetPhysicalPath(path);
            var results = new List<IFileStoreEntry>();

            if (!Directory.Exists(physicalPath))
            {
                return results.ToList();
            }

            // Add directories.
            if (listDirectories)
                results.AddRange(
                    Directory
                        .GetDirectories(physicalPath, "*", includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                        .Select(f =>
                        {
                            var fileSystemInfo = new PhysicalDirectoryInfo(new DirectoryInfo(f));
                            var fileRelativePath = f.Substring(_fileSystemPath.Length);
                            var filePath = this.NormalizePath(fileRelativePath);
                            return new FileSystemStoreEntry(filePath, fileSystemInfo);
                        }));

            // Add files.
            if (listFiles)
                results.AddRange(
                Directory
                    .GetFiles(physicalPath, "*", includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Select(f =>
                    {
                        var fileSystemInfo = new PhysicalFileInfo(new FileInfo(f));
                        var fileRelativePath = f.Substring(_fileSystemPath.Length);
                        var filePath = this.NormalizePath(fileRelativePath);
                        return new FileSystemStoreEntry(filePath, fileSystemInfo);
                    }));

            return results.ToList();
        }

        public bool TryCreateDirectory(string path)
        {
            var physicalPath = GetPhysicalPath(path);

            if (File.Exists(physicalPath))
            {
                throw new Exception($"Cannot create directory because the path '{path}' already exists and is a file.");
            }

            if (Directory.Exists(physicalPath))
            {
                return false;
            }

            Directory.CreateDirectory(physicalPath);

            return true;
        }
        public Task<bool> TryRenameDirectory(string path, string newName)
        {
            var physicalPath = GetPhysicalPath(path);
            
            if (File.Exists(physicalPath))
            {
                throw new Exception($"Cannot create directory because the path '{path}' already exists and is a file.");
            }

            if (!Directory.Exists(physicalPath))
            {
                return Task.FromResult(false);
            }
            var directoryInfo = new DirectoryInfo(physicalPath);

            var newphysicalPath = GetPhysicalPath(Path.Combine(directoryInfo.Parent.FullName, newName));
            if (Directory.Exists(newphysicalPath))
            {
                return Task.FromResult(false);
            }
            
            directoryInfo.MoveTo(newphysicalPath);

            return Task.FromResult(true);
        }

        public Task<bool> TryDeleteFile(string path)
        {
            var physicalPath = GetPhysicalPath(path);

            if (!File.Exists(physicalPath))
            {
                return Task.FromResult(false);
            }

            File.Delete(physicalPath);

            return Task.FromResult(true);
        }

        public Task<bool> TryDeleteDirectory(string path)
        {
            var physicalPath = GetPhysicalPath(path);

            if (!Directory.Exists(physicalPath))
            {
                return Task.FromResult(false);
            }

            Directory.Delete(physicalPath, recursive: true);

            return Task.FromResult(true);
        }

        public Task MoveFile(string oldPath, string newPath)
        {
            var physicalOldPath = GetPhysicalPath(oldPath);

            if (!File.Exists(physicalOldPath))
            {
                throw new Exception($"Cannot move file '{oldPath}' because it does not exist.");
            }

            var physicalNewPath = GetPhysicalPath(newPath);

            if (File.Exists(physicalNewPath) || Directory.Exists(physicalNewPath))
            {
                throw new Exception($"Cannot move file because the new path '{newPath}' already exists.");
            }

            File.Move(physicalOldPath, physicalNewPath);

            return Task.CompletedTask;
        }

        public async Task CopyFile(string srcPath, string dstPath)
        {
            var physicalSrcPath = GetPhysicalPath(srcPath);

            if (!File.Exists(physicalSrcPath))
            {
                throw new Exception($"The file '{srcPath}' does not exist.");
            }
            var file = await GetFileInfo(srcPath);
            var physicalDstPath = GetPhysicalPath(Path.Combine(dstPath, file.Name));

            if (File.Exists(physicalDstPath) || Directory.Exists(physicalDstPath))
            {
                throw new Exception($"Cannot copy file because the destination path '{dstPath}' already exists.");
            }

            File.Copy(physicalSrcPath, physicalDstPath);
        }
        public async Task RenameFile(string file, string newName)
        {
            var physicalSrcFile = GetPhysicalPath(file);

            if (!File.Exists(physicalSrcFile))
            {
                throw new Exception($"The file '{file}' does not exist.");
            }

            var physicalFile = await GetFileInfo(file);

            var newphysicalFile = GetPhysicalPath(Path.Combine(physicalFile.DirectoryPath, newName));
            if (File.Exists(newphysicalFile))
            {
                throw new Exception($"The file '{newName}' does exist.");
            }
            File.Move(physicalSrcFile, newphysicalFile);

        }
        public Task<Stream> GetFileStream(string path)
        {
            var physicalPath = GetPhysicalPath(path);

            if (!File.Exists(physicalPath))
            {
                throw new Exception($"Cannot get file stream because the file '{path}' does not exist.");
            }

            var stream = File.OpenRead(physicalPath);

            return Task.FromResult<Stream>(stream);
        }

        public Task<Stream> GetFileStream(IFileStoreEntry fileStoreEntry)
        {
            var physicalPath = GetPhysicalPath(fileStoreEntry.Path);
            if (!File.Exists(physicalPath))
            {
                throw new Exception($"Cannot get file stream because the file '{fileStoreEntry.Path}' does not exist.");
            }

            var stream = File.OpenRead(physicalPath);

            return Task.FromResult<Stream>(stream);
        }

        public async Task<string> CreateFileFromStream(string path, Stream inputStream, bool overwrite = false)
        {
            var physicalPath = GetPhysicalPath(path);

            if (!overwrite && File.Exists(physicalPath))
            {
                throw new Exception($"Cannot create file '{path}' because it already exists.");
            }

            if (Directory.Exists(physicalPath))
            {
                throw new Exception($"Cannot create file '{path}' because it already exists as a directory.");
            }

            // Create directory path if it doesn't exist.
            var physicalDirectoryPath = Path.GetDirectoryName(physicalPath);
            Directory.CreateDirectory(physicalDirectoryPath);

            var fileInfo = new FileInfo(physicalPath);
            using (var outputStream = fileInfo.Create())
            {
                await inputStream.CopyToAsync(outputStream);
            }

            return path;
        }

        public Task<string> ReadAllText(string path)
        {
            var physicalPath = GetPhysicalPath(path);

            if (!File.Exists(physicalPath))
            {
                throw new Exception($"File not exists.");
            }
            return File.ReadAllTextAsync(physicalPath);
        }

        public Task WriteAllText(string path, string text)
        {
            var physicalPath = GetPhysicalPath(path);

            return File.WriteAllTextAsync(physicalPath, text, Encoding.UTF8);
        }


        /// <summary>
        /// Translates a relative path in the virtual file store to a physical path in the underlying file system.
        /// </summary>
        /// <param name="path">The relative path within the file store.</param>
        /// <returns></returns>
        /// <remarks>The resulting physical path is verified to be inside designated root file system path.</remarks>
        private string GetPhysicalPath(string path)
        {
            path = this.NormalizePath(path);

            var physicalPath = String.IsNullOrEmpty(path) ? _fileSystemPath : Path.Combine(_fileSystemPath, path);

            // Verify that the resulting path is inside the root file system path.
            var pathIsAllowed = Path.GetFullPath(physicalPath).StartsWith(_fileSystemPath, StringComparison.OrdinalIgnoreCase);
            if (!pathIsAllowed)
            {
                throw new Exception($"The path '{path}' resolves to a physical path outside the file system store root.");
            }

            return physicalPath;
        }

    }
}
