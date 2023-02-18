using Grand.Domain.Media;

namespace Grand.Business.Core.Interfaces.Storage
{
    /// <summary>
    /// Download service interface
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        /// Gets a download
        /// </summary>
        /// <param name="downloadId">Download identifier</param>
        /// <returns>Download</returns>
        Task<Download> GetDownloadById(string downloadId);

        /// <summary>
        /// Gets a download by GUID
        /// </summary>
        /// <param name="downloadGuid">Download GUID</param>
        /// <returns>Download</returns>
        Task<Download> GetDownloadByGuid(Guid downloadGuid);

        /// <summary>
        /// Inserts a download
        /// </summary>
        /// <param name="download">Download</param>
        Task InsertDownload(Download download);

        /// <summary>
        /// Updates the download
        /// </summary>
        /// <param name="download">Download</param>
        Task UpdateDownload(Download download);

        /// <summary>
        /// Deletes a download
        /// </summary>
        /// <param name="download">Download</param>
        Task DeleteDownload(Download download);

    }
}
