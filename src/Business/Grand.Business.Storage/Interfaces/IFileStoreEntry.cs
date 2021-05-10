//Source https://github.com/OrchardCMS/OrchardCore/tree/dev/src/OrchardCore
using System;

namespace Grand.Business.Storage.Interfaces
{
    public interface IFileStoreEntry
    {
        /// <summary>
        /// Gets the full path of the file store entry within the file store.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets the name of the file store entry.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the full path of the file store entry's containing directory within the file store.
        /// </summary>
        string DirectoryPath { get; }

        /// <summary>
        /// Gets the full path of the file store entry's containing directory within the file store.
        /// </summary>
        string PhysicalPath { get; }

        /// <summary>
        /// Gets the length of the file (0 if the file story entry is a directory).
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Gets the date and time in UTC when the file store entry was last modified.
        /// </summary>
        DateTime LastModifiedUtc { get; }

        /// <summary>
        /// Gets a boolean indicating whether the file store entry is a directory.
        /// </summary>
        bool IsDirectory { get; }
    }
}
