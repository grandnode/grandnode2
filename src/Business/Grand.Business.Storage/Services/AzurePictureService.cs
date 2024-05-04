using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Storage.Services;

/// <summary>
///     Picture service for Windows Azure
/// </summary>
public class AzurePictureService : PictureService
{
    #region Ctor

    public AzurePictureService(IRepository<Picture> pictureRepository,
        ILogger<AzurePictureService> logger,
        IMediator mediator,
        ICacheBase cacheBase,
        IMediaFileStore mediaFileStore,
        MediaSettings mediaSettings,
        StorageSettings storageSettings,
        AzureConfig config,
        IMimeMappingService mimeMappingService
    )
        : base(pictureRepository,
            logger,
            mediator,
            cacheBase,
            mediaFileStore,
            mediaSettings,
            storageSettings)
    {
        _config = config;
        _mimeMappingService = mimeMappingService;

        if (string.IsNullOrEmpty(_config.AzureBlobStorageConnectionString))
            throw new Exception("Azure connection string for BLOB is not specified");
        if (string.IsNullOrEmpty(_config.AzureBlobStorageContainerName))
            throw new Exception("Azure container name for BLOB is not specified");
        if (string.IsNullOrEmpty(_config.AzureBlobStorageEndPoint))
            throw new Exception("Azure end point for BLOB is not specified");

        _container = new BlobContainerClient(_config.AzureBlobStorageConnectionString,
            _config.AzureBlobStorageContainerName);
    }

    #endregion

    #region Fields

    private static BlobContainerClient _container;
    private readonly AzureConfig _config;
    private readonly IMimeMappingService _mimeMappingService;

    #endregion

    #region Utilities

    /// <summary>
    ///     Delete picture thumbs
    /// </summary>
    /// <param name="picture">Picture</param>
    protected override async Task DeletePictureThumbs(Picture picture)
    {
        var filter = $"{picture.Id}";
        var blobs = _container.GetBlobs(BlobTraits.All, BlobStates.All, filter);

        foreach (var blob in blobs) await _container.DeleteBlobAsync(blob.Name);
    }

    /// <summary>
    ///     Get picture (thumb) local path
    /// </summary>
    /// <param name="thumbFileName">Filename</param>
    /// <returns>Local picture thumb path</returns>
    protected override async Task<string> GetThumbPhysicalPath(string thumbFileName)
    {
        var thumbFilePath =
            $"{_config.AzureBlobStorageEndPoint}{_config.AzureBlobStorageContainerName}/{thumbFileName}";
        var blobClient = _container.GetBlobClient(thumbFileName);
        bool exists = await blobClient.ExistsAsync();
        return exists ? thumbFilePath : string.Empty;
    }

    /// <summary>
    ///     Get picture (thumb) URL
    /// </summary>
    /// <param name="thumbFileName">Filename</param>
    /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
    /// <returns>Local picture thumb path</returns>
    protected override string GetThumbUrl(string thumbFileName, string storeLocation = null)
    {
        var url = _config.AzureBlobStorageEndPoint + _config.AzureBlobStorageContainerName + "/";
        url += thumbFileName;
        return url;
    }

    /// <summary>
    ///     Save a value indicating whether some file (thumb) already exists
    /// </summary>
    /// <param name="thumbFileName">Thumb file name</param>
    /// <param name="binary">Picture binary</param>
    protected override Task SaveThumb(string thumbFileName, byte[] binary)
    {
        Stream stream = new MemoryStream(binary);
        _container.UploadBlob(thumbFileName, stream);

        //Update content type and other properties 
        var contentType = _mimeMappingService.Map(thumbFileName);
        var blobClient = _container.GetBlobClient(thumbFileName);
        BlobProperties properties = blobClient.GetProperties();
        var blobHttpHeaders = new BlobHttpHeaders {
            // Set the MIME ContentType every time the properties 
            // are updated or the field will be cleared
            ContentType = contentType,
            // Populate remaining headers with 
            // the pre-existing properties
            CacheControl = properties.CacheControl,
            ContentDisposition = properties.ContentDisposition,
            ContentEncoding = properties.ContentEncoding,
            ContentHash = properties.ContentHash
        };
        blobClient.SetHttpHeaders(blobHttpHeaders);
        return Task.CompletedTask;
    }

    #endregion
}