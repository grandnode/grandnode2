using Grand.Business.Storage.Interfaces;
using Grand.Domain.Data;
using Grand.Domain.Media;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Storage.Services
{
    /// <summary>
    /// Download service
    /// </summary>
    public partial class DownloadService : IDownloadService
    {
        #region Fields

        private readonly IRepository<Download> _downloadRepository;
        private readonly IDatabaseContext _dbContext;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="downloadRepository">Download repository</param>
        /// <param name="dbContext">DbContext</param>
        /// <param name="mediator">Mediator</param>
        public DownloadService(IRepository<Download> downloadRepository,
            IDatabaseContext dbContext,
            IMediator mediator)
        {
            _downloadRepository = downloadRepository;
            _dbContext = dbContext;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a download
        /// </summary>
        /// <param name="downloadId">Download identifier</param>
        /// <returns>Download</returns>
        public virtual async Task<Download> GetDownloadById(string downloadId)
        {
            if (string.IsNullOrEmpty(downloadId))
                return null;

            var _download = await _downloadRepository.GetByIdAsync(downloadId);
            if (!_download.UseDownloadUrl)
                _download.DownloadBinary = await DownloadAsBytes(_download.DownloadObjectId);

            return _download;
        }

        protected virtual async Task<byte[]> DownloadAsBytes(string objectId)
        {
            var binary = await _dbContext.GridFSBucketDownload(objectId);
            return binary;
        }
        /// <summary>
        /// Gets a download by GUID
        /// </summary>
        /// <param name="downloadGuid">Download GUID</param>
        /// <returns>Download</returns>
        public virtual async Task<Download> GetDownloadByGuid(Guid downloadGuid)
        {
            if (downloadGuid == Guid.Empty)
                return null;

            var query = from o in _downloadRepository.Table
                        where o.DownloadGuid == downloadGuid
                        select o;

            var order = query.FirstOrDefault();
            if (!order.UseDownloadUrl)
                order.DownloadBinary = await DownloadAsBytes(order.DownloadObjectId);

            return await Task.FromResult(order);
        }

        /// <summary>
        /// Inserts a download
        /// </summary>
        /// <param name="download">Download</param>
        public virtual async Task InsertDownload(Download download)
        {
            if (download == null)
                throw new ArgumentNullException(nameof(download));
            if (!download.UseDownloadUrl)
            {
                download.DownloadObjectId = await _dbContext.GridFSBucketUploadFromBytesAsync(download.Filename, download.DownloadBinary);
            }

            download.DownloadBinary = null;
            await _downloadRepository.InsertAsync(download);

            await _mediator.EntityInserted(download);
        }

        /// <summary>
        /// Updates the download
        /// </summary>
        /// <param name="download">Download</param>
        public virtual async Task UpdateDownload(Download download)
        {
            if (download == null)
                throw new ArgumentNullException(nameof(download));

            await _downloadRepository.UpdateAsync(download);

            await _mediator.EntityUpdated(download);
        }

        /// <summary>
        /// Deletes a download
        /// </summary>
        /// <param name="download">Download</param>
        public virtual async Task DeleteDownload(Download download)
        {
            if (download == null)
                throw new ArgumentNullException(nameof(download));

            await _downloadRepository.DeleteAsync(download);

            await _mediator.EntityDeleted(download);
        }

        #endregion
    }
}
