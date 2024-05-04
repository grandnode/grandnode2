using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Grand.Business.Storage.Services;

/// <summary>
///     Picture service for Amazon
/// </summary>
public class AmazonPictureService : PictureService
{
    #region Ctor

    public AmazonPictureService(IRepository<Picture> pictureRepository,
        ILogger<AmazonPictureService> logger,
        IMediator mediator,
        ICacheBase cacheBase,
        IMediaFileStore mediaFileStore,
        MediaSettings mediaSettings,
        StorageSettings storageSettings,
        AmazonConfig config)
        : base(pictureRepository,
            logger,
            mediator,
            cacheBase,
            mediaFileStore,
            mediaSettings,
            storageSettings)
    {
        _config = config;

        //Arguments guard
        if (string.IsNullOrEmpty(_config.AmazonAwsAccessKeyId))
            throw new ArgumentNullException("AmazonAwsAccessKeyId");
        if (string.IsNullOrEmpty(_config.AmazonAwsSecretAccessKey))
            throw new ArgumentNullException("AmazonAwsSecretAccessKey");
        if (string.IsNullOrEmpty(_config.AmazonBucketName))
            throw new ArgumentNullException("AmazonBucketName");

        //Region guard
        var regionEndpoint = RegionEndpoint.GetBySystemName(_config.AmazonRegion);
        if (regionEndpoint.DisplayName == "Unknown")
            throw new NullReferenceException("specified Region is invalid");

        //Client guard
        _s3Client = new AmazonS3Client(_config.AmazonAwsAccessKeyId, _config.AmazonAwsSecretAccessKey, regionEndpoint);

        //Bucket guard
        _bucketName = _config.AmazonBucketName;

        //Cloudfront distribution
        _distributionDomainName = _config.AmazonDistributionDomainName;
    }

    #endregion

    #region Fields

    private readonly AmazonConfig _config;
    private readonly string _bucketName;
    private readonly string _distributionDomainName;
    private bool _bucketExist;
    private readonly IAmazonS3 _s3Client;

    #endregion

    #region Utilities

    private async Task CheckBucketExists()
    {
        if (!_bucketExist)
        {
            _bucketExist = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
            while (_bucketExist == false)
            {
                var s3Region = S3Region.FindValue(_config.AmazonRegion);
                var putBucketRequest = new PutBucketRequest {
                    BucketName = _bucketName,
                    BucketRegion = s3Region
                };

                try
                {
                    EnsureValidResponse(await _s3Client.PutBucketAsync(putBucketRequest), HttpStatusCode.OK);
                }
                catch (AmazonS3Exception ex)
                {
                    if (ex.ErrorCode == "BucketAlreadyOwnedByYou")
                        break;

                    throw;
                }

                _bucketExist = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
            }
        }
    }

    /// <summary>
    ///     Ensure Every Response Will Have Expected HttpStatusCode
    /// </summary>
    /// <param name="actualResponse">Actual Response</param>
    /// <param name="expectedHttpStatusCode">Expected Status Code</param>
    private void EnsureValidResponse(AmazonWebServiceResponse actualResponse, HttpStatusCode expectedHttpStatusCode)
    {
        if (actualResponse.HttpStatusCode != expectedHttpStatusCode)
            throw new Exception("Http Status Codes Aren't Consistent");
    }

    /// <summary>
    ///     Delete picture thumbs
    /// </summary>
    /// <param name="picture">Picture</param>
    protected override async Task DeletePictureThumbs(Picture picture)
    {
        await CheckBucketExists();

        var listObjectsRequest = new ListObjectsV2Request {
            BucketName = _bucketName,
            Prefix = picture.Id
        };
        var listObjectsResponse = await _s3Client.ListObjectsV2Async(listObjectsRequest);

        foreach (var s3Object in listObjectsResponse.S3Objects)
            EnsureValidResponse(await _s3Client.DeleteObjectAsync(_bucketName, s3Object.Key), HttpStatusCode.NoContent);
    }

    /// <summary>
    ///     Get picture (thumb) local path
    /// </summary>
    /// <param name="thumbFileName">Filename</param>
    /// <returns>Local picture thumb path</returns>
    protected override async Task<string> GetThumbPhysicalPath(string thumbFileName)
    {
        if (!await GeneratedThumbExists(thumbFileName))
            return null;

        if (string.IsNullOrEmpty(_distributionDomainName))
            return await Task.FromResult($"https://{_bucketName}.s3.amazonAws.com/{thumbFileName}");
        return await Task.FromResult($"https://{_distributionDomainName}/{thumbFileName}");
    }

    /// <summary>
    ///     Get a value indicating whether some file (thumb) already exists
    /// </summary>
    /// <param name="thumbFileName">Thumb file name</param>
    /// <returns>Result</returns>
    private Task<bool> GeneratedThumbExists(string thumbFileName)
    {
        try
        {
            var getObjectResponse = _s3Client.GetObjectAsync(_bucketName, thumbFileName).GetAwaiter().GetResult();
            EnsureValidResponse(getObjectResponse, HttpStatusCode.OK);

            return Task.FromResult(
                getObjectResponse.BucketName == _bucketName || getObjectResponse.Key == thumbFileName);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    ///     Get picture (thumb) URL
    /// </summary>
    /// <param name="thumbFileName">Filename</param>
    /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
    /// <returns>Local picture thumb path</returns>
    protected override string GetThumbUrl(string thumbFileName, string storeLocation = null)
    {
        return string.IsNullOrEmpty(_distributionDomainName)
            ? $"https://{_bucketName}.s3.amazonAws.com/{thumbFileName}"
            : $"https://{_distributionDomainName}/{thumbFileName}";
    }

    /// <summary>
    ///     Save a value indicating whether some file (thumb) already exists
    /// </summary>
    /// <param name="thumbFileName">Thumb file name</param>
    /// <param name="binary">Picture binary</param>
    protected override Task SaveThumb(string thumbFileName, byte[] binary)
    {
        CheckBucketExists().Wait();

        using (Stream stream = new MemoryStream(binary))
        {
            var putObjectRequest = new PutObjectRequest {
                BucketName = _bucketName,
                InputStream = stream,
                Key = thumbFileName,
                StorageClass = S3StorageClass.Standard
            };
            _s3Client.PutObjectAsync(putObjectRequest).Wait();
        }

        _s3Client.MakeObjectPublicAsync(_bucketName, thumbFileName, true).Wait();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Clears pictures stored on Amazon S3, it won't affect Pictures stored in database
    /// </summary>
    public override async Task ClearThumbs()
    {
        await CheckBucketExists();

        var listObjectsRequest = new ListObjectsV2Request {
            BucketName = _bucketName
        };
        var listObjectsResponse = await _s3Client.ListObjectsV2Async(listObjectsRequest);

        foreach (var s3Object in listObjectsResponse.S3Objects)
            EnsureValidResponse(await _s3Client.DeleteObjectAsync(_bucketName, s3Object.Key), HttpStatusCode.NoContent);
    }

    #endregion
}