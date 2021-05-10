//Source https://github.com/OrchardCMS/OrchardCore/tree/dev/src/OrchardCore

using Microsoft.Extensions.FileProviders.Physical;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Storage.Interfaces
{
    /// <summary>
    /// Represents a generic abstraction over a virtual file store.
    /// </summary>
    public interface IFileStore
    {
        /// <summary>
        /// Retrieves information about the given file within the file store.
        /// </summary>
        /// <param name="path">The path within the file store.</param>
        /// <returns>A <see cref="IFileStoreEntry"/> object representing the file, or <c>null</c> if the file does not exist.</returns>
        Task<IFileStoreEntry> GetFileInfo(string path);

        /// <summary>
        /// Retrieves information about the given directory within the file store.
        /// </summary>
        /// <param name="path">The path within the file store.</param>
        /// <returns>A <see cref="IFileStoreEntry"/> object representing the directory, or <c>null</c> if the directory does not exist.</returns>
        IFileStoreEntry GetDirectoryInfo(string path);

        /// <summary>
        /// Retrieves information about the given directory within the file store.
        /// </summary>
        /// <param name="path">The path within the file store.</param>
        /// <returns>A <see cref="PhysicalDirectoryInfo"/> object representing the directory, or <c>null</c> if the directory does not exist.</returns>
        Task<PhysicalDirectoryInfo> GetPhysicalDirectoryInfo(string directorypath);

        /// <summary>
        /// Enumerates the content (files and directories) in a given directory within the file store.
        /// </summary>
        /// <remarks>
        /// Results are grouped by entry type, where directories are followed by files.
        /// </remarks>
        IList<IFileStoreEntry> GetDirectoryContent(string path = null, bool includeSubDirectories = false, bool listDirectories = true, bool listFiles = true);

        /// <summary>
        /// Creates a directory in the file store if it doesn't already exist.
        /// </summary>
        /// <param name="path">The path of the directory to be created.</param>
        /// <returns><c>true</c> if the directory was created; <c>false</c> if the directory already existed.</returns>
        bool TryCreateDirectory(string path);

        /// <summary>
        /// Rename a directory in the file store if it doesn't already exist.
        /// </summary>
        /// <param name="path">The path of the directory to be created.</param>
        /// <param name="newName">The new name of the directory</param>
        /// <returns><c>true</c> if the directory was rename; <c>false</c> if the directory already existed.</returns>
        Task<bool> TryRenameDirectory(string path, string newName);

        /// <summary>
        /// Deletes a file in the file store if it exists.
        /// </summary>
        /// <param name="path">The path of the file to be deleted.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        Task<bool> TryDeleteFile(string path);

        /// <summary>
        /// Deletes a directory in the file store if it exists.
        /// </summary>
        /// <param name="path">The path of the directory to be deleted.</param>
        Task<bool> TryDeleteDirectory(string path);

        /// <summary>
        /// Renames or moves a file to another location in the file store.
        /// </summary>
        /// <param name="oldPath">The path of the file to be renamed/moved.</param>
        /// <param name="newPath">The new path of the file after the rename/move.</param>
        Task MoveFile(string oldPath, string newPath);

        /// <summary>
        /// Creates a copy of a file in the file store.
        /// </summary>
        /// <param name="srcPath">The path of the source file to be copied.</param>
        /// <param name="dstPath">The path of the destination file to be created.</param>
        Task CopyFile(string srcPath, string dstPath);

        /// <summary>
        /// Rename of a file in the file store.
        /// </summary>
        /// <param name="file">The source file.</param>
        /// <param name="newName">The destination file.</param>
        Task RenameFile(string file, string newName);

        /// <summary>
        /// Creates a stream to read the contents of a file in the file store.
        /// </summary>
        /// <param name="path">The path of the file to be read.</param>
        /// <returns>An instance of <see cref="System.IO.Stream"/> that can be used to read the contents of the file. The caller must close the stream when finished.</returns>
        /// <exception cref="FileStoreException">Thrown if the specified file does not exist.</exception>
        Task<Stream> GetFileStream(string path);

        /// <summary>
        /// Creates a stream to read the contents of a file in the file store.
        /// </summary>
        /// <param name="fileStoreEntry">The IFileStoreEntry to be read.</param>
        /// <returns>An instance of <see cref="System.IO.Stream"/> that can be used to read the contents of the file. The caller must close the stream when finished.</returns>
        /// <exception cref="FileStoreException">Thrown if the specified file does not exist.</exception>
        Task<Stream> GetFileStream(IFileStoreEntry fileStoreEntry);

        /// <summary>
        /// Creates a new file in the file store from the contents of an input stream.
        /// </summary>
        /// <param name="path">The path of the file to be created.</param>
        /// <param name="inputStream">The stream whose contents to write to the new file.</param>
        /// <param name="overwrite"><c>true</c> to overwrite if a file already exists; <c>false</c> to throw an exception if the file already exists.</param>
        /// <exception cref="FileStoreException">Thrown if the specified file already exists and <paramref name="overwrite"/> was not set to <c>true</c>, or if the specified path exists but is not a file.</exception>
        /// <remarks>
        /// If the specified path contains one or more directories, then those directories are
        /// created if they do not already exist.
        /// </remarks>
        Task<string> CreateFileFromStream(string path, Stream inputStream, bool overwrite = false);

        /// <summary>
        /// Get text from file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<string> ReadAllText(string path);

        /// <summary>
        /// Write text to file
        /// </summary>
        /// <param name="path"></param>
        /// <param text=text></param>
        /// <returns></returns>
        Task WriteAllText(string path, string text);
    }

    public static class IFileStoreExtensions
    {
        /// <summary>
        /// Combines multiple path parts using the path delimiter semantics of the abstract virtual file store.
        /// </summary>
        /// <param name="fileStore">The <see cref="IFileStore"/>.</param>
        /// <param name="paths">The path parts to combine.</param>
        /// <returns>The full combined path.</returns>
        public static string Combine(this IFileStore fileStore, params string[] paths)
        {
            if (paths.Length == 0)
                return null;

            var normalizedParts =
                paths
                    .Select(x => fileStore.NormalizePath(x))
                    .Where(x => !String.IsNullOrEmpty(x))
                    .ToArray();

            var combined = String.Join("/", normalizedParts);

            // Preserve the initial '/' if it's present.
            if (paths[0]?.StartsWith('/') == true)
                combined = "/" + combined;

            return combined;
        }

        /// <summary>
        /// Normalizes a path using the path delimiter semantics of the abstract virtual file store.
        /// </summary>
        /// <remarks>
        /// Backslash is converted to forward slash and any leading or trailing slashes
        /// are removed.
        /// </remarks>
        public static string NormalizePath(this IFileStore fileStore, string path)
        {
            if (path == null)
                return null;

            return path.Replace('\\', '/').Trim('/', ' ');
        }
    }
}
