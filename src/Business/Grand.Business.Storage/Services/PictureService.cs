using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.Linq.Expressions;

namespace Grand.Business.Storage.Services;

/// <summary>
///     Picture service
/// </summary>
public class PictureService : IPictureService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="pictureRepository">Picture repository</param>
    /// <param name="logger">Logger</param>
    /// <param name="mediator">Mediator</param>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="mediaFileStore">Media file storage</param>
    /// <param name="mediaSettings">Media settings</param>
    /// <param name="storageSettings">Storage settings</param>
    public PictureService(IRepository<Picture> pictureRepository,
        ILogger<PictureService> logger,
        IMediator mediator,
        ICacheBase cacheBase,
        IMediaFileStore mediaFileStore,
        MediaSettings mediaSettings,
        StorageSettings storageSettings)
    {
        _pictureRepository = pictureRepository;
        _logger = logger;
        _mediator = mediator;
        _cacheBase = cacheBase;
        _mediaFileStore = mediaFileStore;
        _mediaSettings = mediaSettings;
        _storageSettings = storageSettings;
    }

    #endregion

    #region Fields

    private readonly IRepository<Picture> _pictureRepository;
    private readonly ILogger<PictureService> _logger;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly IMediaFileStore _mediaFileStore;
    private readonly MediaSettings _mediaSettings;
    private readonly StorageSettings _storageSettings;

    #endregion

    #region Utilities

    /// <summary>
    ///     Returns the file extension from mime type.
    /// </summary>
    /// <param name="mimeType">Mime type</param>
    /// <returns>File extension</returns>
    private string GetFileExtensionFromMimeType(string mimeType)
    {
        if (mimeType == null)
            return null;

        var parts = mimeType.Split('/');
        var lastPart = parts[^1];
        switch (lastPart)
        {
            case "pjpeg":
                lastPart = "jpg";
                break;
            case "x-png":
                lastPart = "png";
                break;
            case "x-icon":
                lastPart = "ico";
                break;
        }

        return lastPart;
    }

    /// <summary>
    ///     Loads a picture from file
    /// </summary>
    /// <param name="pictureId">Picture identifier</param>
    /// <param name="mimeType">MIME type</param>
    /// <returns>Picture binary</returns>
    protected async Task<byte[]> LoadPictureFromFile(string pictureId, string mimeType)
    {
        var lastPart = GetFileExtensionFromMimeType(mimeType);
        var fileName = $"{pictureId}_0.{lastPart}";
        var filePath = await GetPicturePhysicalPath(fileName);
        if (string.IsNullOrEmpty(filePath))
            return Array.Empty<byte>();

        return await File.ReadAllBytesAsync(filePath);
    }


    /// <summary>
    ///     Delete picture thumbs
    /// </summary>
    /// <param name="picture">Picture</param>
    protected virtual Task DeletePictureThumbs(Picture picture)
    {
        var filter = $"{picture.Id}*.*";
        var thumbDirectoryPath = _mediaFileStore.GetDirectoryInfo(CommonPath.ImageThumbPath);
        if (thumbDirectoryPath == null) return Task.CompletedTask;
        var currentFiles = Directory.GetFiles(thumbDirectoryPath.PhysicalPath, filter, SearchOption.AllDirectories);
        foreach (var currentFileName in currentFiles)
            try
            {
                if (File.Exists(currentFileName))
                    File.Delete(currentFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Get picture (thumb) physical path
    /// </summary>
    /// <param name="thumbFileName">Filename</param>
    /// <returns>Local picture physical path</returns>
    protected virtual async Task<string> GetThumbPhysicalPath(string thumbFileName)
    {
        var thumbFile = _mediaFileStore.Combine(CommonPath.ImageThumbPath, thumbFileName);
        var fileInfo = await _mediaFileStore.GetFileInfo(thumbFile);
        return fileInfo?.PhysicalPath;
    }

    /// <summary>
    ///     Get picture (thumb) URL
    /// </summary>
    /// <param name="thumbFileName">Filename</param>
    /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
    /// <returns>Local picture thumb path</returns>
    protected virtual string GetThumbUrl(string thumbFileName, string storeLocation = null)
    {
        storeLocation = !string.IsNullOrEmpty(storeLocation) ? storeLocation : "";
        return _mediaFileStore.Combine(storeLocation, CommonPath.Param, CommonPath.ImageThumbPath, thumbFileName);
    }

    /// <summary>
    ///     Get picture physical path. Used when images stored on file system (not in the database)
    /// </summary>
    /// <param name="fileName">Filename</param>
    /// <returns>Physical picture path</returns>
    protected virtual async Task<string> GetPicturePhysicalPath(string fileName)
    {
        var fileInfo = await _mediaFileStore.GetFileInfo(_mediaFileStore.Combine(CommonPath.ImagePath, fileName));
        return fileInfo?.PhysicalPath;
    }

    /// <summary>
    ///     Gets the loaded picture binary depending on picture storage settings
    /// </summary>
    /// <param name="picture">Picture</param>
    /// <param name="fromDb">Load from database; otherwise, from file system</param>
    /// <returns>Picture binary</returns>
    public virtual async Task<byte[]> LoadPictureBinary(Picture picture, bool fromDb)
    {
        ArgumentNullException.ThrowIfNull(picture);

        var result = fromDb
            ? (await _pictureRepository.GetByIdAsync(picture.Id)).PictureBinary
            : await LoadPictureFromFile(picture.Id, picture.MimeType);

        return result;
    }

    /// <summary>
    ///     Save a value indicating whether some file (thumb) already exists
    /// </summary>
    /// <param name="thumbFileName">Thumb file name</param>
    /// <param name="binary">Picture binary</param>
    protected virtual Task SaveThumb(string thumbFileName, byte[] binary)
    {
        try
        {
            var dirThumb = _mediaFileStore.GetDirectoryInfo(CommonPath.ImageThumbPath);
            if (dirThumb == null)
            {
                var result = _mediaFileStore.TryCreateDirectory(CommonPath.ImageThumbPath);
                if (result)
                    dirThumb = _mediaFileStore.GetDirectoryInfo(CommonPath.ImageThumbPath);
            }

            if (dirThumb != null)
            {
                var file = _mediaFileStore.Combine(dirThumb.PhysicalPath, thumbFileName);
                File.WriteAllBytes(file, binary ?? Array.Empty<byte>());
            }
            else
            {
                _logger.LogError("Directory thumb not exist");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Getting picture local path/URL methods

    /// <summary>
    ///     Gets the loaded picture binary depending on picture storage settings
    /// </summary>
    /// <param name="picture">Picture</param>
    /// <returns>Picture binary</returns>
    public virtual async Task<byte[]> LoadPictureBinary(Picture picture)
    {
        return await LoadPictureBinary(picture, _storageSettings.PictureStoreInDb);
    }

    /// <summary>
    ///     Get picture SEO friendly name
    /// </summary>
    /// <param name="name">Name</param>
    /// <returns>Result</returns>
    public virtual string GetPictureSeName(string name)
    {
        return SeoExtensions.GenerateSlug(name, true, false, false);
    }

    /// <summary>
    ///     Gets the default picture URL
    /// </summary>
    /// <param name="targetSize">The target picture size (longest side)</param>
    /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
    /// <returns>Picture URL</returns>
    public virtual async Task<string> GetDefaultPictureUrl(int targetSize = 0, string storeLocation = null)
    {
        var filePath = await GetPicturePhysicalPath(_mediaSettings.DefaultImageName);
        if (string.IsNullOrEmpty(filePath)) return _mediaFileStore.Combine(CommonPath.ImagePath, "no-image.png");
        if (targetSize == 0)
            return !string.IsNullOrEmpty(storeLocation)
                ? storeLocation
                : _mediaFileStore.Combine(CommonPath.ImagePath, _mediaSettings.DefaultImageName);

        var fileExtension = Path.GetExtension(filePath);
        var thumbFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{targetSize}{fileExtension}";

        var thumbFilePath = await GetThumbPhysicalPath(thumbFileName);

        if (!string.IsNullOrEmpty(thumbFilePath))
            return GetThumbUrl(thumbFileName, storeLocation);

        using (var mutex = new Mutex(false, thumbFileName))
        {
            mutex.WaitOne();
            using (var image = SKBitmap.Decode(filePath))
            {
                var pictureBinary = ApplyResize(image, EncodedImageFormat(fileExtension), targetSize);
                if (pictureBinary != null)
                    await SaveThumb(thumbFileName, pictureBinary);
            }

            mutex.ReleaseMutex();
        }

        var url = GetThumbUrl(thumbFileName, storeLocation);
        return url;
    }

    /// <summary>
    ///     Get a picture URL
    /// </summary>
    /// <param name="pictureId">Picture identifier</param>
    /// <param name="targetSize">The target picture size (longest side)</param>
    /// <param name="showDefaultPicture">A value indicating whether the default picture is shown</param>
    /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
    /// <returns>Picture URL</returns>
    public virtual async Task<string> GetPictureUrl(string pictureId,
        int targetSize = 0,
        bool showDefaultPicture = true,
        string storeLocation = null)
    {
        var pictureKey = string.Format(CacheKey.PICTURE_BY_KEY, pictureId, targetSize, showDefaultPicture,
            storeLocation);
        return await _cacheBase.GetAsync(pictureKey, async () =>
        {
            var picture = await GetPictureById(pictureId);
            return await GetPictureUrl(picture, targetSize, showDefaultPicture, storeLocation);
        });
    }

    /// <summary>
    ///     Get a picture URL
    /// </summary>
    /// <param name="picture">Picture instance</param>
    /// <param name="targetSize">The target picture size (longest side)</param>
    /// <param name="showDefaultPicture">A value indicating whether the default picture is shown</param>
    /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
    /// <returns>Picture URL</returns>
    public virtual async Task<string> GetPictureUrl(Picture picture,
        int targetSize = 0,
        bool showDefaultPicture = true,
        string storeLocation = null)
    {
        if (picture == null)
            return showDefaultPicture ? await GetDefaultPictureUrl(targetSize, storeLocation) : string.Empty;

        if (picture.IsNew)
        {
            await DeletePictureThumbs(picture);

            picture.IsNew = false;
            await _pictureRepository.UpdateField(picture.Id, x => x.IsNew, picture.IsNew);
        }

        var seoFileName = picture.SeoFilename;
        var lastPart = GetFileExtensionFromMimeType(picture.MimeType);
        string thumbFileName;

        if (targetSize == 0)
        {
            thumbFileName = !string.IsNullOrEmpty(seoFileName)
                ? $"{picture.Id}_{seoFileName}.{lastPart}"
                : $"{picture.Id}.{lastPart}";

            var thumbFilePath = await GetThumbPhysicalPath(thumbFileName);

            if (!string.IsNullOrEmpty(thumbFilePath))
                return GetThumbUrl(thumbFileName, storeLocation);

            var pictureBinary = await LoadPictureBinary(picture);

            using var mutex = new Mutex(false, thumbFileName);
            mutex.WaitOne();
            await SaveThumb(thumbFileName, pictureBinary);
            mutex.ReleaseMutex();
        }
        else
        {
            thumbFileName = !string.IsNullOrEmpty(seoFileName)
                ? $"{picture.Id}_{seoFileName}_{targetSize}.{lastPart}"
                : $"{picture.Id}_{targetSize}.{lastPart}";

            var thumbFilePath = await GetThumbPhysicalPath(thumbFileName);

            if (!string.IsNullOrEmpty(thumbFilePath))
                return GetThumbUrl(thumbFileName, storeLocation);

            var pictureBinary = await LoadPictureBinary(picture);

            using var mutex = new Mutex(false, thumbFileName);
            mutex.WaitOne();
            if (pictureBinary != null)
                try
                {
                    using var image = SKBitmap.Decode(pictureBinary);
                    var resizedBinary = ApplyResize(image, EncodedImageFormat(picture.MimeType), targetSize);
                    if (resizedBinary != null)
                        pictureBinary = resizedBinary;
                }
                catch
                {
                    // ignored
                }

            await SaveThumb(thumbFileName, pictureBinary);

            mutex.ReleaseMutex();
        }

        return GetThumbUrl(thumbFileName, storeLocation);
    }

    /// <summary>
    ///     Get a picture physical path
    /// </summary>
    /// <param name="picture">Picture instance</param>
    /// <param name="targetSize">The target picture size (longest side)</param>
    /// <param name="showDefaultPicture">A value indicating whether the default picture is shown</param>
    /// <returns></returns>
    public virtual async Task<string> GetThumbPhysicalPath(Picture picture, int targetSize = 0,
        bool showDefaultPicture = true)
    {
        var url = await GetPictureUrl(picture, targetSize, showDefaultPicture);
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        return await GetThumbPhysicalPath(Path.GetFileName(url));
    }

    #endregion

    #region CRUD methods

    /// <summary>
    ///     Gets a picture
    /// </summary>
    /// <param name="pictureId">Picture identifier</param>
    /// <returns>Picture</returns>
    public virtual async Task<Picture> GetPictureById(string pictureId)
    {
        var pictureKey = string.Format(CacheKey.PICTURE_BY_ID, pictureId);
        return await _cacheBase.GetAsync(pictureKey, async () =>
        {
            var query = _pictureRepository.Table
                .Where(p => p.Id == pictureId)
                .Select(p =>
                    new Picture {
                        Id = p.Id,
                        AltAttribute = p.AltAttribute,
                        IsNew = p.IsNew,
                        MimeType = p.MimeType,
                        SeoFilename = p.SeoFilename,
                        TitleAttribute = p.TitleAttribute,
                        Reference = p.Reference,
                        ObjectId = p.ObjectId,
                        Locales = p.Locales,
                        Style = p.Style,
                        ExtraField = p.ExtraField
                    });
            return await Task.FromResult(query.FirstOrDefault());
        });
    }

    /// <summary>
    ///     Deletes a picture
    /// </summary>
    /// <param name="picture">Picture</param>
    public virtual async Task DeletePicture(Picture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);

        //delete thumbs
        await DeletePictureThumbs(picture);

        //delete from file system
        if (!_storageSettings.PictureStoreInDb)
            await DeletePictureOnFileSystem(picture);

        //delete from database
        await _pictureRepository.DeleteAsync(picture);

        //event notification
        await _mediator.EntityDeleted(picture);
    }

    /// <summary>
    ///     Delete a picture on file system
    /// </summary>
    /// <param name="picture">Picture</param>
    public virtual async Task DeletePictureOnFileSystem(Picture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);

        var lastPart = GetFileExtensionFromMimeType(picture.MimeType);
        var fileName = $"{picture.Id}_0.{lastPart}";
        var filePath = await GetPicturePhysicalPath(fileName);
        if (!string.IsNullOrEmpty(filePath)) File.Delete(filePath);
    }

    public virtual async Task ClearThumbs()
    {
        const string searchPattern = "*.*";
        var path = _mediaFileStore.GetDirectoryInfo(CommonPath.ImageThumbPath)?.PhysicalPath;

        if (!Directory.Exists(path))
            return;

        foreach (var file in Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories))
        {
            if (file.Contains("placeholder.txt"))
                continue;
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Gets a collection of pictures
    /// </summary>
    /// <param name="pageIndex">Current page</param>
    /// <param name="pageSize">Items on each page</param>
    /// <returns>Paged list of pictures</returns>
    public virtual IPagedList<Picture> GetPictures(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = from p in _pictureRepository.Table
            select p;
        var pictures = new PagedList<Picture>(query, pageIndex, pageSize);
        return pictures;
    }

    /// <summary>
    ///     Inserts a picture
    /// </summary>
    /// <param name="pictureBinary">The picture binary</param>
    /// <param name="mimeType">The picture MIME type</param>
    /// <param name="seoFilename">The SEO filename</param>
    /// <param name="altAttribute">"alt" attribute for "img" HTML element</param>
    /// <param name="titleAttribute">"title" attribute for "img" HTML element</param>
    /// <param name="isNew">A value indicating whether the picture is new</param>
    /// <param name="reference">Reference type</param>
    /// <param name="objectId">Object id for reference</param>
    /// <param name="validateBinary">A value indicating whether to validated provided picture binary</param>
    /// <returns>Picture</returns>
    public virtual async Task<Picture> InsertPicture(byte[] pictureBinary, string mimeType, string seoFilename,
        string altAttribute = null, string titleAttribute = null,
        bool isNew = true, Reference reference = Reference.None, string objectId = "", bool validateBinary = false)
    {
        mimeType = CommonHelper.EnsureNotNull(mimeType);
        mimeType = CommonHelper.EnsureMaximumLength(mimeType, 20);

        seoFilename = CommonHelper.EnsureMaximumLength(seoFilename, 100);

        if (validateBinary)
            pictureBinary = ValidatePicture(pictureBinary, mimeType);

        var picture = new Picture {
            PictureBinary = _storageSettings.PictureStoreInDb ? pictureBinary : Array.Empty<byte>(),
            MimeType = mimeType,
            SeoFilename = seoFilename,
            AltAttribute = altAttribute,
            TitleAttribute = titleAttribute,
            Reference = reference,
            ObjectId = objectId,
            IsNew = isNew
        };
        await _pictureRepository.InsertAsync(picture);

        if (!_storageSettings.PictureStoreInDb)
            await SavePictureInFile(picture.Id, pictureBinary, mimeType);

        //event notification
        await _mediator.EntityInserted(picture);

        return picture;
    }

    /// <summary>
    ///     Updates the picture
    /// </summary>
    /// <param name="pictureId">The picture identifier</param>
    /// <param name="pictureBinary">The picture binary</param>
    /// <param name="mimeType">The picture MIME type</param>
    /// <param name="seoFilename">The SEO filename</param>
    /// <param name="altAttribute">"alt" attribute for "img" HTML element</param>
    /// <param name="titleAttribute">"title" attribute for "img" HTML element</param>
    /// <param name="style">style attribute for "img" HTML element</param>
    /// <param name="extraField">Extra field</param>
    /// <param name="isNew">A value indicating whether the picture is new</param>
    /// <param name="validateBinary">A value indicating whether to validated provided picture binary</param>
    /// <returns>Picture</returns>
    public virtual async Task<Picture> UpdatePicture(string pictureId, byte[] pictureBinary, string mimeType,
        string seoFilename, string altAttribute = null, string titleAttribute = null,
        string style = null, string extraField = null,
        bool isNew = true, bool validateBinary = true)
    {
        mimeType = CommonHelper.EnsureNotNull(mimeType);
        mimeType = CommonHelper.EnsureMaximumLength(mimeType, 20);

        seoFilename = CommonHelper.EnsureMaximumLength(seoFilename, 100);

        if (validateBinary && pictureBinary != null)
            pictureBinary = ValidatePicture(pictureBinary, mimeType);

        var picture = await GetPictureById(pictureId);
        if (picture == null)
            return null;

        //delete old thumbs if a picture has been changed
        if (seoFilename != picture.SeoFilename || pictureBinary != null)
            await DeletePictureThumbs(picture);

        if (pictureBinary != null)
        {
            picture.PictureBinary = _storageSettings.PictureStoreInDb ? pictureBinary : Array.Empty<byte>();
            await _pictureRepository.UpdateField(picture.Id, x => x.PictureBinary, picture.PictureBinary);
        }

        picture.MimeType = mimeType;
        await _pictureRepository.UpdateField(picture.Id, x => x.MimeType, picture.MimeType);

        picture.SeoFilename = seoFilename;
        await _pictureRepository.UpdateField(picture.Id, x => x.SeoFilename, picture.SeoFilename);

        picture.AltAttribute = altAttribute;
        await _pictureRepository.UpdateField(picture.Id, x => x.AltAttribute, picture.AltAttribute);

        picture.TitleAttribute = titleAttribute;
        await _pictureRepository.UpdateField(picture.Id, x => x.TitleAttribute, picture.TitleAttribute);

        picture.Style = style;
        await _pictureRepository.UpdateField(picture.Id, x => x.Style, picture.Style);

        picture.ExtraField = extraField;
        await _pictureRepository.UpdateField(picture.Id, x => x.ExtraField, picture.ExtraField);

        picture.IsNew = isNew;
        await _pictureRepository.UpdateField(picture.Id, x => x.IsNew, picture.IsNew);

        if (!_storageSettings.PictureStoreInDb && pictureBinary != null)
            await SavePictureInFile(picture.Id, pictureBinary, mimeType);

        //event notification
        await _mediator.EntityUpdated(picture);

        //clare cache
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PICTURE_BY_ID, picture.Id));

        return picture;
    }

    /// <summary>
    ///     Updates the picture
    /// </summary>
    /// <param name="picture">Picture</param>
    /// <returns>Picture</returns>
    public virtual async Task<Picture> UpdatePicture(Picture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);

        await _pictureRepository.UpdateAsync(picture);

        //event notification
        await _mediator.EntityUpdated(picture);

        //clare cache
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PICTURE_BY_ID, picture.Id));

        return picture;
    }

    /// <summary>
    ///     Updates the picture
    /// </summary>
    /// <param name="picture">Picture</param>
    /// <param name="expression"></param>
    /// <param name="value"></param>
    /// <returns>Picture</returns>
    public virtual async Task UpdatePictureField<T>(Picture picture, Expression<Func<Picture, T>> expression, T value)
    {
        ArgumentNullException.ThrowIfNull(picture);

        await _pictureRepository.UpdateField(picture.Id, expression, value);

        //event notification
        await _mediator.EntityUpdated(picture);

        //clare cache
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PICTURE_BY_ID, picture.Id));
    }

    /// <summary>
    ///     Save picture on file system
    /// </summary>
    /// <param name="pictureId">Picture identifier</param>
    /// <param name="pictureBinary">Picture binary</param>
    /// <param name="mimeType">MIME type</param>
    public virtual Task SavePictureInFile(string pictureId, byte[] pictureBinary, string mimeType)
    {
        var lastPart = GetFileExtensionFromMimeType(mimeType);
        var fileName = $"{pictureId}_0.{lastPart}";
        var dirPath = _mediaFileStore.GetDirectoryInfo(CommonPath.ImagePath);
        if (dirPath != null)
        {
            var filepath = _mediaFileStore.Combine(dirPath.PhysicalPath, fileName);
            File.WriteAllBytes(filepath, pictureBinary);
        }
        else
        {
            _logger.LogError("Directory path not exist");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Updates a SEO filename of a picture
    /// </summary>
    /// <param name="picture">The picture</param>
    /// <param name="seoFilename">The SEO filename</param>
    /// <returns>Picture</returns>
    public virtual async Task<Picture> SetSeoFilename(Picture picture, string seoFilename)
    {
        if (picture == null)
            throw new ArgumentException("No picture found with the specified id");

        //update if it has been changed
        if (seoFilename == picture.SeoFilename) return picture;
        //update SeoFilename picture
        picture.SeoFilename = seoFilename;
        await UpdatePictureField(picture, p => p.SeoFilename, seoFilename);

        //event notification
        await _mediator.EntityUpdated(picture);

        //clare cache
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PICTURE_BY_ID, picture.Id));
        return picture;
    }

    /// <summary>
    ///     Validates input picture dimensions
    /// </summary>
    /// <param name="byteArray">Picture binary</param>
    /// <param name="mimeType">MIME type</param>
    /// <returns>Picture binary or throws an exception</returns>
    public virtual byte[] ValidatePicture(byte[] byteArray, string mimeType)
    {
        try
        {
            var format = EncodedImageFormat(mimeType);
            using (new MemoryStream(byteArray))
            {
                using (var image = SKBitmap.Decode(byteArray))
                {
                    if (image.Width >= image.Height)
                    {
                        //horizontal rectangle or square
                        if (image.Width <= _mediaSettings.MaximumImageSize ||
                            image.Height <= _mediaSettings.MaximumImageSize) return byteArray;
                        var resizedPicture = ApplyResize(image, format, _mediaSettings.MaximumImageSize);
                        if (resizedPicture != null)
                            byteArray = resizedPicture;
                    }
                    else if (image.Width < image.Height)
                    {
                        //vertical rectangle
                        if (image.Width <= _mediaSettings.MaximumImageSize) return byteArray;
                        var resizedPicture = ApplyResize(image, format, _mediaSettings.MaximumImageSize);
                        if (resizedPicture != null)
                            byteArray = resizedPicture;
                    }

                    return byteArray;
                }
            }
        }
        catch
        {
            return byteArray;
        }
    }


    /// <summary>
    ///     Convert picture
    /// </summary>
    /// <param name="pictureBinary">Picture binary</param>
    /// <param name="imageQuality">Image quality</param>
    /// <param name="format">Format</param>
    /// <returns>Picture binary or throws an exception</returns>
    public virtual byte[] ConvertPicture(byte[] pictureBinary, int imageQuality, string format = "Webp")
    {
        Enum.TryParse(typeof(SKEncodedImageFormat), format, out var skFormat);
        skFormat ??= SKEncodedImageFormat.Webp;

        using var image = SKBitmap.Decode(pictureBinary);
        var d = SKImage.FromBitmap(image).Encode((SKEncodedImageFormat)skFormat, imageQuality);
        return d.ToArray();
    }


    private SKEncodedImageFormat EncodedImageFormat(string mimetype)
    {
        const SKEncodedImageFormat defaultFormat = SKEncodedImageFormat.Jpeg;
        if (string.IsNullOrEmpty(mimetype))
            return defaultFormat;

        mimetype = mimetype.ToLower();

        if (mimetype.Contains("jpeg"))
            return defaultFormat;

        if (mimetype.Contains("jpg"))
            return defaultFormat;

        if (mimetype.Contains("pjpeg"))
            return defaultFormat;

        if (mimetype.Contains("png"))
            return SKEncodedImageFormat.Png;

        if (mimetype.Contains("webp"))
            return SKEncodedImageFormat.Webp;

        if (mimetype.Contains("wbmp"))
            return SKEncodedImageFormat.Wbmp;

        if (mimetype.Contains("gif"))
            return SKEncodedImageFormat.Gif;

        //if mime type is BMP format then happens error with convert picture
        if (mimetype.Contains("bmp"))
            return SKEncodedImageFormat.Png;

        return mimetype.Contains("ico") ? SKEncodedImageFormat.Ico : defaultFormat;
    }

    private byte[] ApplyResize(SKBitmap image, SKEncodedImageFormat format, int targetSize)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (targetSize <= 0) targetSize = 800;
        float width, height;
        if (image.Height > image.Width)
        {
            // portrait
            width = image.Width * (targetSize / (float)image.Height);
            height = targetSize;
        }
        else
        {
            // landscape or square
            width = targetSize;
            height = image.Height * (targetSize / (float)image.Width);
        }

        if ((int)width == 0 || (int)height == 0)
        {
            width = image.Width;
            height = image.Height;
        }

        try
        {
            using var resized = image.Resize(new SKImageInfo((int)width, (int)height), SKFilterQuality.High);
            using var resImage = SKImage.FromBitmap(resized);
            var skData = resImage.Encode(format, _mediaSettings.ImageQuality);
            return skData?.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApplyResize - format {Format}", format);
            return null;
        }
    }

    #endregion
}