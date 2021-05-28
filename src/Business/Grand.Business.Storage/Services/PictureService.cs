using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Media;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Infrastructure.Extensions;
using Grand.Business.Common.Extensions;
using Grand.SharedKernel.Extensions;
using Grand.Domain.Common;

namespace Grand.Business.Storage.Services
{
    /// <summary>
    /// Picture service
    /// </summary>
    public partial class PictureService : IPictureService
    {

        #region Fields

        private readonly IRepository<Picture> _pictureRepository;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IWorkContext _workContext;
        private readonly ICacheBase _cacheBase;
        private readonly IMediaFileStore _mediaFileStore;
        private readonly MediaSettings _mediaSettings;

        //Used when images stored on file system
        private readonly string _imagePath;

        //Used for thumb images
        private readonly string _thumbPath;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="pictureRepository">Picture repository</param>
        /// <param name="logger">Logger</param>
        /// <param name="mediator">Mediator</param>
        /// <param name="hostingEnvironment">hostingEnvironment</param>
        /// <param name="workContext">Current context</param>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="mediaFileStore">Media file storage</param>
        /// <param name="mediaSettings">Media settings</param>
        public PictureService(IRepository<Picture> pictureRepository,
            ILogger logger,
            IMediator mediator,
            IWebHostEnvironment hostingEnvironment,
            IWorkContext workContext,
            ICacheBase cacheBase,
            IMediaFileStore mediaFileStore,
            MediaSettings mediaSettings)
        {
            _pictureRepository = pictureRepository;
            _logger = logger;
            _mediator = mediator;
            _hostingEnvironment = hostingEnvironment;
            _workContext = workContext;
            _cacheBase = cacheBase;
            _mediaFileStore = mediaFileStore;
            _mediaSettings = mediaSettings;

            _imagePath = _mediaFileStore.Combine("assets", "images");
            _thumbPath = _mediaFileStore.Combine("assets", "images", "thumbs");
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Returns the file extension from mime type.
        /// </summary>
        /// <param name="mimeType">Mime type</param>
        /// <returns>File extension</returns>
        protected virtual string GetFileExtensionFromMimeType(string mimeType)
        {
            if (mimeType == null)
                return null;

            string[] parts = mimeType.Split('/');
            string lastPart = parts[parts.Length - 1];
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
        /// Loads a picture from file
        /// </summary>
        /// <param name="pictureId">Picture identifier</param>
        /// <param name="mimeType">MIME type</param>
        /// <returns>Picture binary</returns>
        protected virtual async Task<byte[]> LoadPictureFromFile(string pictureId, string mimeType)
        {
            string lastPart = GetFileExtensionFromMimeType(mimeType);
            string fileName = string.Format("{0}_0.{1}", pictureId, lastPart);
            var filePath = await GetPicturePhysicalPath(fileName);
            if (string.IsNullOrEmpty(filePath))
                return new byte[0];

            return File.ReadAllBytes(filePath);
        }


        /// <summary>
        /// Delete picture thumbs
        /// </summary>
        /// <param name="picture">Picture</param>
        protected virtual Task DeletePictureThumbs(Picture picture)
        {
            string filter = string.Format("{0}*.*", picture.Id);
            var thumbDirectoryPath = _mediaFileStore.GetDirectoryInfo(_thumbPath);
            if (thumbDirectoryPath != null)
            {
                string[] currentFiles = Directory.GetFiles(thumbDirectoryPath.PhysicalPath, filter, SearchOption.AllDirectories);
                foreach (string currentFileName in currentFiles)
                {
                    try
                    {
                        if (!File.Exists(currentFileName))
                            File.Delete(currentFileName);
                    }
                    catch { }
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get picture (thumb) physical path
        /// </summary>
        /// <param name="thumbFileName">Filename</param>
        /// <returns>Local picture physical path</returns>
        protected virtual async Task<string> GetThumbPhysicalPath(string thumbFileName)
        {
            var thumbFile = _mediaFileStore.Combine(_thumbPath, thumbFileName);
            var fileInfo = await _mediaFileStore.GetFileInfo(thumbFile);
            return fileInfo?.PhysicalPath;
        }

        /// <summary>
        /// Get picture (thumb) URL 
        /// </summary>
        /// <param name="thumbFileName">Filename</param>
        /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
        /// <returns>Local picture thumb path</returns>
        protected virtual string GetThumbUrl(string thumbFileName, string storeLocation = null)
        {
            storeLocation = !string.IsNullOrEmpty(storeLocation)
                                    ? storeLocation
                                    : string.IsNullOrEmpty(_mediaSettings.StoreLocation) ?
                                    _workContext.CurrentStore.SslEnabled ? _workContext.CurrentStore.SecureUrl : _workContext.CurrentStore.Url :
                                    _mediaSettings.StoreLocation;

            return _mediaFileStore.Combine(storeLocation, CommonPath.Param, _thumbPath, thumbFileName);
        }

        /// <summary>
        /// Get picture physical path. Used when images stored on file system (not in the database)
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <returns>Physical picture path</returns>
        protected virtual async Task<string> GetPicturePhysicalPath(string fileName)
        {
            var fileinfo = await _mediaFileStore.GetFileInfo(_mediaFileStore.Combine(_imagePath, fileName));
            return fileinfo?.PhysicalPath;
        }

        /// <summary>
        /// Gets the loaded picture binary depending on picture storage settings
        /// </summary>
        /// <param name="picture">Picture</param>
        /// <param name="fromDb">Load from database; otherwise, from file system</param>
        /// <returns>Picture binary</returns>
        public virtual async Task<byte[]> LoadPictureBinary(Picture picture, bool fromDb)
        {
            if (picture == null)
                throw new ArgumentNullException(nameof(picture));

            var result = fromDb
                ? (await _pictureRepository.GetByIdAsync(picture.Id)).PictureBinary
                : await LoadPictureFromFile(picture.Id, picture.MimeType);

            return result;
        }

        /// <summary>
        /// Save a value indicating whether some file (thumb) already exists
        /// </summary>
        /// <param name="thumbFileName">Thumb file name</param>
        /// <param name="binary">Picture binary</param>
        protected virtual Task SaveThumb(string thumbFileName, byte[] binary)
        {
            try
            {
                var dirThumb = _mediaFileStore.GetDirectoryInfo(_thumbPath);
                if (dirThumb == null)
                {
                    var result = _mediaFileStore.TryCreateDirectory(_thumbPath);
                    if (result)
                        dirThumb = _mediaFileStore.GetDirectoryInfo(_thumbPath);
                }

                if (dirThumb != null)
                {
                    var file = _mediaFileStore.Combine(dirThumb.PhysicalPath, thumbFileName);
                    File.WriteAllBytes(file, binary ?? Array.Empty<byte>());
                }
                else
                    _logger.InsertLog(Domain.Logging.LogLevel.Error, "Directory thumb not exist.");

            }
            catch (Exception ex)
            {
                _logger.InsertLog(Domain.Logging.LogLevel.Error, ex.Message);
            }
            return Task.CompletedTask;
        }


        #endregion

        #region Getting picture local path/URL methods

        /// <summary>
        /// Gets the loaded picture binary depending on picture storage settings
        /// </summary>
        /// <param name="picture">Picture</param>
        /// <returns>Picture binary</returns>
        public virtual async Task<byte[]> LoadPictureBinary(Picture picture)
        {
            return await LoadPictureBinary(picture, _mediaSettings.StoreInDb);
        }

        /// <summary>
        /// Get picture SEO friendly name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Result</returns>
        public virtual string GetPictureSeName(string name)
        {
            return SeoExtensions.GenerateSlug(name, true, false, false);
        }

        /// <summary>
        /// Gets the default picture URL
        /// </summary>
        /// <param name="targetSize">The target picture size (longest side)</param>
        /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
        /// <returns>Picture URL</returns>
        public virtual async Task<string> GetDefaultPictureUrl(int targetSize = 0, string storeLocation = null)
        {
            var filePath = await GetPicturePhysicalPath(_mediaSettings.DefaultImageName);
            if (string.IsNullOrEmpty(filePath))
            {
                return "";
            }
            if (targetSize == 0)
            {
                return !string.IsNullOrEmpty(storeLocation)
                        ? storeLocation
                        : string.IsNullOrEmpty(_mediaSettings.StoreLocation) ?
                        _workContext.CurrentStore.SslEnabled ? _workContext.CurrentStore.SecureUrl : _workContext.CurrentStore.Url :
                        _mediaFileStore.Combine(_mediaSettings.StoreLocation, _imagePath, _mediaSettings.DefaultImageName);
            }
            else
            {
                string fileExtension = Path.GetExtension(filePath);
                string thumbFileName = string.Format("{0}_{1}{2}",
                    Path.GetFileNameWithoutExtension(filePath),
                    targetSize,
                    fileExtension);

                var thumbFilePath = await GetThumbPhysicalPath(thumbFileName);

                if (!string.IsNullOrEmpty(thumbFilePath))
                    return GetThumbUrl(thumbFileName, storeLocation);

                using (var mutex = new Mutex(false, thumbFileName))
                {
                    mutex.WaitOne();
                    using (var image = SKBitmap.Decode(filePath))
                    {
                        var pictureBinary = ApplyResize(image, EncodedImageFormat(fileExtension), targetSize);
                        await SaveThumb(thumbFileName, pictureBinary);
                    }
                    mutex.ReleaseMutex();
                }
                var url = GetThumbUrl(thumbFileName, storeLocation);
                return url;
            }
        }

        /// <summary>
        /// Get a picture URL
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
            var pictureKey = string.Format(CacheKey.PICTURE_BY_KEY, pictureId, _workContext.CurrentStore?.Id, targetSize, showDefaultPicture, storeLocation);
            return await _cacheBase.GetAsync(pictureKey, async () =>
            {
                var picture = await GetPictureById(pictureId);
                return await GetPictureUrl(picture, targetSize, showDefaultPicture, storeLocation);
            });
        }

        /// <summary>
        /// Get a picture URL
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
            {
                return showDefaultPicture ? await GetDefaultPictureUrl(targetSize, storeLocation) : string.Empty;
            }

            byte[] pictureBinary = null;

            if (picture.IsNew)
            {
                if ((picture.PictureBinary?.Length ?? 0) == 0)
                    pictureBinary = await LoadPictureBinary(picture);
                else
                    pictureBinary = picture.PictureBinary;

                await DeletePictureThumbs(picture);

                //we do not validate picture binary here to ensure that no exception ("Parameter is not valid") will be thrown
                picture = await UpdatePicture(picture.Id,
                    pictureBinary,
                    picture.MimeType,
                    picture.SeoFilename,
                    picture.AltAttribute,
                    picture.TitleAttribute,
                    false,
                    false);
            }

            string seoFileName = picture.SeoFilename;
            string lastPart = GetFileExtensionFromMimeType(picture.MimeType);
            string thumbFileName;

            if (targetSize == 0)
            {
                thumbFileName = !string.IsNullOrEmpty(seoFileName) ?
                    string.Format("{0}_{1}.{2}", picture.Id, seoFileName, lastPart) :
                    string.Format("{0}.{1}", picture.Id, lastPart);

                var thumbFilePath = await GetThumbPhysicalPath(thumbFileName);

                if (!string.IsNullOrEmpty(thumbFilePath))
                    return GetThumbUrl(thumbFileName, storeLocation);

                pictureBinary ??= await LoadPictureBinary(picture);

                using (var mutex = new Mutex(false, thumbFileName))
                {
                    mutex.WaitOne();

                    await SaveThumb(thumbFileName, pictureBinary);

                    mutex.ReleaseMutex();
                }
            }
            else
            {
                thumbFileName = !string.IsNullOrEmpty(seoFileName) ?
                    string.Format("{0}_{1}_{2}.{3}", picture.Id, seoFileName, targetSize, lastPart) :
                    string.Format("{0}_{1}.{2}", picture.Id, targetSize, lastPart);

                var thumbFilePath = await GetThumbPhysicalPath(thumbFileName);

                if (!string.IsNullOrEmpty(thumbFilePath))
                    return GetThumbUrl(thumbFileName, storeLocation);

                pictureBinary ??= await LoadPictureBinary(picture);

                using (var mutex = new Mutex(false, thumbFileName))
                {
                    mutex.WaitOne();
                    if (pictureBinary != null)
                    {
                        try
                        {
                            using (var image = SKBitmap.Decode(pictureBinary))
                            {
                                pictureBinary = ApplyResize(image, EncodedImageFormat(picture.MimeType), targetSize);
                            }
                        }
                        catch { }
                    }
                    await SaveThumb(thumbFileName, pictureBinary);

                    mutex.ReleaseMutex();
                }
            }
            return GetThumbUrl(thumbFileName, storeLocation);
        }

        /// <summary>
        /// Get a picture physical path
        /// </summary>
        /// <param name="picture">Picture instance</param>
        /// <param name="targetSize">The target picture size (longest side)</param>
        /// <param name="showDefaultPicture">A value indicating whether the default picture is shown</param>
        /// <returns></returns>
        public virtual async Task<string> GetThumbPhysicalPath(Picture picture, int targetSize = 0, bool showDefaultPicture = true)
        {
            string url = await GetPictureUrl(picture, targetSize, showDefaultPicture);
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            return await GetThumbPhysicalPath(Path.GetFileName(url));
        }

        #endregion

        #region CRUD methods

        /// <summary>
        /// Gets a picture
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
                        new Picture
                        {
                            Id = p.Id,
                            AltAttribute = p.AltAttribute,
                            IsNew = p.IsNew,
                            MimeType = p.MimeType,
                            SeoFilename = p.SeoFilename,
                            TitleAttribute = p.TitleAttribute,
                            Reference = p.Reference,
                            ObjectId = p.ObjectId
                        });
                return await Task.FromResult(query.FirstOrDefault());
            });
        }

        /// <summary>
        /// Deletes a picture
        /// </summary>
        /// <param name="picture">Picture</param>
        public virtual async Task DeletePicture(Picture picture)
        {
            if (picture == null)
                throw new ArgumentNullException(nameof(picture));

            //delete thumbs
            await DeletePictureThumbs(picture);

            //delete from file system
            if (!_mediaSettings.StoreInDb)
                await DeletePictureOnFileSystem(picture);

            //delete from database
            await _pictureRepository.DeleteAsync(picture);

            //event notification
            await _mediator.EntityDeleted(picture);
        }

        /// <summary>
        /// Delete a picture on file system
        /// </summary>
        /// <param name="picture">Picture</param>
        public virtual async Task DeletePictureOnFileSystem(Picture picture)
        {
            if (picture == null)
                throw new ArgumentNullException(nameof(picture));

            var lastPart = GetFileExtensionFromMimeType(picture.MimeType);
            var fileName = string.Format("{0}_0.{1}", picture.Id, lastPart);
            var filePath = await GetPicturePhysicalPath(fileName);
            if (!string.IsNullOrEmpty(filePath))
            {
                File.Delete(filePath);
            }
        }

        public virtual async Task ClearThumbs()
        {
            const string searchPattern = "*.*";
            string path = Path.Combine(_hostingEnvironment.WebRootPath, "content/images/thumbs");

            if (!System.IO.Directory.Exists(path))
                return;

            foreach (string str in System.IO.Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories))
            {
                if (str.Contains("placeholder.txt"))
                    continue;
                try
                {
                    File.Delete(await GetThumbPhysicalPath(str));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message, ex);
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets a collection of pictures
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
        /// Inserts a picture
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

            var picture = new Picture
            {
                PictureBinary = _mediaSettings.StoreInDb ? pictureBinary : new byte[0],
                MimeType = mimeType,
                SeoFilename = seoFilename,
                AltAttribute = altAttribute,
                TitleAttribute = titleAttribute,
                Reference = reference,
                ObjectId = objectId,
                IsNew = isNew,
            };
            await _pictureRepository.InsertAsync(picture);

            if (!_mediaSettings.StoreInDb)
                await SavePictureInFile(picture.Id, pictureBinary, mimeType);

            //event notification
            await _mediator.EntityInserted(picture);

            return picture;
        }

        /// <summary>
        /// Updates the picture
        /// </summary>
        /// <param name="pictureId">The picture identifier</param>
        /// <param name="pictureBinary">The picture binary</param>
        /// <param name="mimeType">The picture MIME type</param>
        /// <param name="seoFilename">The SEO filename</param>
        /// <param name="altAttribute">"alt" attribute for "img" HTML element</param>
        /// <param name="titleAttribute">"title" attribute for "img" HTML element</param>
        /// <param name="isNew">A value indicating whether the picture is new</param>
        /// <param name="validateBinary">A value indicating whether to validated provided picture binary</param>
        /// <returns>Picture</returns>
        public virtual async Task<Picture> UpdatePicture(string pictureId, byte[] pictureBinary, string mimeType,
            string seoFilename, string altAttribute = null, string titleAttribute = null,
            bool isNew = true, bool validateBinary = true)
        {
            mimeType = CommonHelper.EnsureNotNull(mimeType);
            mimeType = CommonHelper.EnsureMaximumLength(mimeType, 20);

            seoFilename = CommonHelper.EnsureMaximumLength(seoFilename, 100);

            if (validateBinary)
                pictureBinary = ValidatePicture(pictureBinary, mimeType);

            var picture = await GetPictureById(pictureId);
            if (picture == null)
                return null;

            //delete old thumbs if a picture has been changed
            if (seoFilename != picture.SeoFilename)
                await DeletePictureThumbs(picture);

            picture.PictureBinary = _mediaSettings.StoreInDb ? pictureBinary : new byte[0];
            picture.MimeType = mimeType;
            picture.SeoFilename = seoFilename;
            picture.AltAttribute = altAttribute;
            picture.TitleAttribute = titleAttribute;
            picture.IsNew = isNew;

            await _pictureRepository.UpdateAsync(picture);

            if (!_mediaSettings.StoreInDb)
                await SavePictureInFile(picture.Id, pictureBinary, mimeType);

            //event notification
            await _mediator.EntityUpdated(picture);

            //clare cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PICTURE_BY_ID, picture.Id));

            return picture;
        }

        /// <summary>
        /// Updates the picture
        /// </summary>
        /// <param name="picture">Picture</param>
        /// <returns>Picture</returns>
        public virtual async Task<Picture> UpdatePicture(Picture picture)
        {
            if (picture == null)
                throw new ArgumentNullException(nameof(picture));

            await _pictureRepository.UpdateAsync(picture);

            //event notification
            await _mediator.EntityUpdated(picture);

            //clare cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PICTURE_BY_ID, picture.Id));

            return picture;
        }

        /// <summary>
        /// Save picture on file system
        /// </summary>
        /// <param name="pictureId">Picture identifier</param>
        /// <param name="pictureBinary">Picture binary</param>
        /// <param name="mimeType">MIME type</param>
        public virtual Task SavePictureInFile(string pictureId, byte[] pictureBinary, string mimeType)
        {
            var lastPart = GetFileExtensionFromMimeType(mimeType);
            var fileName = string.Format("{0}_0.{1}", pictureId, lastPart);
            var dirpath = _mediaFileStore.GetDirectoryInfo(_imagePath);
            if (dirpath != null)
            {
                var filepath = _mediaFileStore.Combine(dirpath.PhysicalPath, fileName);
                File.WriteAllBytes(filepath, pictureBinary);
            }
            else
                _logger.Error("Drirectory path not exist.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates a SEO filename of a picture
        /// </summary>
        /// <param name="pictureId">The picture identifier</param>
        /// <param name="seoFilename">The SEO filename</param>
        /// <returns>Picture</returns>
        public virtual async Task<Picture> SetSeoFilename(string pictureId, string seoFilename)
        {
            var picture = await GetPictureById(pictureId);
            if (picture == null)
                throw new ArgumentException("No picture found with the specified id");

            //update if it has been changed
            if (seoFilename != picture.SeoFilename)
            {
                //update picture
                picture = await UpdatePicture(picture.Id,
                    await LoadPictureBinary(picture),
                    picture.MimeType,
                    seoFilename,
                    picture.AltAttribute,
                    picture.TitleAttribute,
                    true,
                    false);
            }
            return picture;
        }

        /// <summary>
        /// Validates input picture dimensions
        /// </summary>
        /// <param name="pictureBinary">Picture binary</param>
        /// <param name="mimeType">MIME type</param>
        /// <returns>Picture binary or throws an exception</returns>
        public virtual byte[] ValidatePicture(byte[] byteArray, string mimeType)
        {
            try
            {
                var format = EncodedImageFormat(mimeType);
                using (var ms = new MemoryStream(byteArray))
                {
                    using (var image = SKBitmap.Decode(byteArray))
                    {
                        if (image.Width >= image.Height)
                        {
                            //horizontal rectangle or square
                            if (image.Width > _mediaSettings.MaximumImageSize && image.Height > _mediaSettings.MaximumImageSize)
                                byteArray = ApplyResize(image, format, _mediaSettings.MaximumImageSize);
                        }
                        else if (image.Width < image.Height)
                        {
                            //vertical rectangle
                            if (image.Width > _mediaSettings.MaximumImageSize)
                                byteArray = ApplyResize(image, format, _mediaSettings.MaximumImageSize);
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
        protected SKEncodedImageFormat EncodedImageFormat(string mimetype)
        {
            SKEncodedImageFormat defaultFormat = SKEncodedImageFormat.Jpeg;
            if (string.IsNullOrEmpty(mimetype))
                return defaultFormat;

            mimetype = mimetype.ToLower();

            if (mimetype.Contains("jpeg") || mimetype.Contains("jpg") || mimetype.Contains("pjpeg"))
                return defaultFormat;

            if (mimetype.Contains("png"))
                return SKEncodedImageFormat.Png;

            if (mimetype.Contains("webp"))
                return SKEncodedImageFormat.Webp;

            if (mimetype.Contains("webp"))
                return SKEncodedImageFormat.Webp;

            if (mimetype.Contains("gif"))
                return SKEncodedImageFormat.Gif;

            //if mime type is BMP format then happens error with convert picture
            if (mimetype.Contains("bmp"))
                return SKEncodedImageFormat.Png;

            if (mimetype.Contains("ico"))
                return SKEncodedImageFormat.Ico;

            return defaultFormat;

        }
        protected byte[] ApplyResize(SKBitmap image, SKEncodedImageFormat format, int targetSize)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if (targetSize <= 0)
            {
                targetSize = 800;
            }
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
                using (var resized = image.Resize(new SKImageInfo((int)width, (int)height), SKFilterQuality.None))
                {
                    using (var resimage = SKImage.FromBitmap(resized))
                    {
                        return resimage.Encode(format, 100).ToArray();
                    }
                }
            }
            catch
            {
                return image.Bytes;
            }


        }

        #endregion

    }
}