using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Domain.Media;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Storage.Services;

/// <summary>
///     Download service
/// </summary>
public class DownloadService : IDownloadService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="downloadRepository">Download repository</param>
    /// <param name="storeFilesContext">Store Files Context</param>
    /// <param name="mediator">Mediator</param>
    public DownloadService(IRepository<Download> downloadRepository,
        IStoreFilesContext storeFilesContext,
        IMediator mediator)
    {
        _downloadRepository = downloadRepository;
        _storeFilesContext = storeFilesContext;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<Download> _downloadRepository;
    private readonly IStoreFilesContext _storeFilesContext;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets a download
    /// </summary>
    /// <param name="downloadId">Download identifier</param>
    /// <returns>Download</returns>
    public virtual async Task<Download> GetDownloadById(string downloadId)
    {
        if (string.IsNullOrEmpty(downloadId))
            return null;

        var download = await _downloadRepository.GetByIdAsync(downloadId);
        if (download is { UseDownloadUrl: false })
            download.DownloadBinary = await DownloadAsBytes(download.DownloadObjectId);

        return download;
    }

    protected virtual async Task<byte[]> DownloadAsBytes(string objectId)
    {
        var binary = await _storeFilesContext.BucketDownload(objectId);
        return binary;
    }

    /// <summary>
    ///     Gets a download by GUID
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
        if (order is { UseDownloadUrl: false })
            order.DownloadBinary = await DownloadAsBytes(order.DownloadObjectId);

        return await Task.FromResult(order);
    }

    /// <summary>
    ///     Inserts a download
    /// </summary>
    /// <param name="download">Download</param>
    public virtual async Task InsertDownload(Download download)
    {
        ArgumentNullException.ThrowIfNull(download);
        if (!download.UseDownloadUrl)
            download.DownloadObjectId =
                await _storeFilesContext.BucketUploadFromBytes(download.Filename, download.DownloadBinary);

        download.DownloadBinary = null;
        await _downloadRepository.InsertAsync(download);

        await _mediator.EntityInserted(download);
    }

    /// <summary>
    ///     Updates the download
    /// </summary>
    /// <param name="download">Download</param>
    public virtual async Task UpdateDownload(Download download)
    {
        ArgumentNullException.ThrowIfNull(download);

        await _downloadRepository.UpdateAsync(download);

        await _mediator.EntityUpdated(download);
    }

    /// <summary>
    ///     Deletes a download
    /// </summary>
    /// <param name="download">Download</param>
    public virtual async Task DeleteDownload(Download download)
    {
        ArgumentNullException.ThrowIfNull(download);

        await _downloadRepository.DeleteAsync(download);

        //delete from bucket
        if (!string.IsNullOrEmpty(download.DownloadObjectId))
            await _storeFilesContext.BucketDelete(download.DownloadObjectId);

        await _mediator.EntityDeleted(download);
    }

    #endregion
}