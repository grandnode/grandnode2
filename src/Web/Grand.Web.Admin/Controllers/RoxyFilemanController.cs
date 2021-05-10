using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Storage.Interfaces;
using Grand.Business.Storage.Services;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Models.RoxyFileman;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    //Controller for Roxy fileman (http://www.roxyfileman.com/) for TinyMCE editor
    //do not validate request token (XSRF)
    [PermissionAuthorize(PermissionSystemName.Files)]
    public class RoxyFilemanController : BaseAdminController
    {
        #region Constants

        /// <summary>
        /// Path to directory of language files
        /// </summary>
        private const string LANGUAGE_DIRECTORY = "/_content/Grand.Web.Admin/administration/roxy_fileman/lang";

        /// <summary>
        /// Path to configuration file
        /// </summary>
        private const string CONFIGURATION_FILE = "/_content/Grand.Web.Admin/administration/roxy_fileman/conf.json";

        #endregion

        #region Fields

        private Dictionary<string, string> _settings;
        private Dictionary<string, string> _languageResources;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IPermissionService _permissionService;

        private readonly IMediaFileStore _mediaFileStore;

        #endregion

        #region Ctor

        public RoxyFilemanController(
            IWebHostEnvironment hostingEnvironment,
            IPermissionService permissionService)
        {
            _hostingEnvironment = hostingEnvironment;
            _permissionService = permissionService;

            var fullPathToUpload = Path.Combine(CommonPath.WebRootPath, Path.Combine("assets", "images", "uploaded"));
            if (!Directory.Exists(fullPathToUpload))
                Directory.CreateDirectory(fullPathToUpload);

            var fullPath = Path.Combine(CommonPath.WebRootPath, Path.Combine("assets", "images"));
            var fileStore = new FileSystemStore(fullPath);
            _mediaFileStore = new DefaultMediaFileStore(fileStore);

        }

        #endregion

        #region Methods

        /// <summary>
        /// Process request
        /// </summary>
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> ProcessRequest()
        {
            var action = "DIRLIST";
            try
            {
                if (!await _permissionService.Authorize(StandardPermission.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

                if (!StringValues.IsNullOrEmpty(HttpContext.Request.Query["a"]))
                    action = HttpContext.Request.Query["a"];

                return action.ToUpper() switch
                {
                    "DIRLIST" => await ListDirTree(HttpContext.Request.Query["type"]),
                    "FILESLIST" => await ListFiles(HttpContext.Request.Query["d"], HttpContext.Request.Query["type"]),
                    "COPYFILE" => await CopyFile(HttpContext.Request.Query["f"], HttpContext.Request.Query["n"]),
                    "CREATEDIR" => await CreateDir(HttpContext.Request.Query["d"], HttpContext.Request.Query["n"]),
                    "DELETEDIR" => await DeleteDir(HttpContext.Request.Query["d"]),
                    "DELETEFILE" => await DeleteFile(HttpContext.Request.Query["f"]),
                    "DOWNLOAD" => await DownloadFile(HttpContext.Request.Query["f"]),
                    "MOVEFILE" => await MoveFile(HttpContext.Request.Query["f"], HttpContext.Request.Query["n"]),
                    "RENAMEDIR" => await RenameDir(HttpContext.Request.Query["d"], HttpContext.Request.Query["n"]),
                    "RENAMEFILE" => await RenameFile(HttpContext.Request.Query["f"], HttpContext.Request.Query["n"]),
                    "GENERATETHUMB" => await CreateThumbnail(HttpContext.Request.Query["f"]),
                    "UPLOAD" => await UploadFile(),
                    _ => Ok(GetErrorResponse("This action is not implemented.")),
                };
            }
            catch (Exception ex)
            {
                if (action == "UPLOAD" && !await IsAjaxRequest())
                    return Ok(GetErrorResponse(ex.Message));
                else
                    return Ok(GetErrorResponse(ex.Message));
            }
        }

        #endregion

        #region Utitlies

        /// <summary>
        /// Get a value of the configuration setting
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <returns>Setting value</returns>
        private string GetSetting(string key)
        {
            var settingsFile = _hostingEnvironment.WebRootFileProvider.GetFileInfo(CONFIGURATION_FILE);
            if (_settings == null)
                _settings = ParseJson(settingsFile.PhysicalPath);

            if (_settings.TryGetValue(key, out string value))
                return value;

            return null;
        }

        /// <summary>
        /// Get the language resource value
        /// </summary>
        /// <param name="key">Language resource key</param>
        /// <returns>Language resource value</returns>
        private string GetLanguageResource(string key)
        {
            var langFile = _hostingEnvironment.WebRootFileProvider.GetFileInfo(GetLanguagePath(false));
            if (!langFile.Exists)
            {
                langFile = _hostingEnvironment.WebRootFileProvider.GetFileInfo(GetLanguagePath(true));
            }
            if (_languageResources == null)
                _languageResources = ParseJson(langFile.PhysicalPath);

            if (_languageResources.TryGetValue(key, out string value))
                return value;

            return key;
        }

        /// <summary>
        /// Get the absolute path to the language resources file
        /// </summary>
        /// <returns>Path</returns>
        private string GetLanguagePath(bool getDefault = false)
        {
            if (getDefault)
            {
                return $"{LANGUAGE_DIRECTORY}/en.json"; ;
            }
            var languageCode = GetSetting("LANG");
            return $"{LANGUAGE_DIRECTORY}/{languageCode}.json";
        }

        /// <summary>
        /// Parse the JSON file
        /// </summary>
        /// <param name="file">Path to the file</param>
        /// <returns>Collection of keys and values from the parsed file</returns>
        protected virtual Dictionary<string, string> ParseJson(string file)
        {
            var result = new Dictionary<string, string>();
            var json = string.Empty;
            try
            {
                json = System.IO.File.ReadAllText(file, System.Text.Encoding.UTF8)?.Trim();
            }
            catch { }

            if (string.IsNullOrEmpty(json))
                return result;

            if (json.StartsWith("{"))
                json = json.Substring(1, json.Length - 2);

            json = json.Trim();
            json = json.Substring(1, json.Length - 2);

            var lines = Regex.Split(json, "\"\\s*,\\s*\"");
            foreach (var line in lines)
            {
                var tmp = Regex.Split(line, "\"\\s*:\\s*\"");
                try
                {
                    if (!string.IsNullOrEmpty(tmp[0]) && !result.ContainsKey(tmp[0]))
                        result.Add(tmp[0], tmp[1]);
                }
                catch { }
            }

            return result;
        }

        /// <summary>
        /// Get a file type by file extension
        /// </summary>
        /// <param name="fileExtension">File extension</param>
        /// <returns>File type</returns>
        protected virtual string GetFileType(string fileExtension)
        {
            var fileType = "file";

            fileExtension = fileExtension.ToLower();
            if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".gif" || fileExtension == ".bmp" || fileExtension == ".webp")
                fileType = "image";

            if (fileExtension == ".swf" || fileExtension == ".flv")
                fileType = "flash";

            if (fileExtension == ".mp4" || fileExtension == ".avi" || fileExtension == ".mov")
                fileType = "media";

            return fileType;
        }

        private bool CanAllowedFile(string path)
        {
            var result = false;
            var fileExtension = new FileInfo(path).Extension.Replace(".", "").ToLower();

            var forbiddenUploads = GetSetting("FORBIDDEN_UPLOADS").Trim().ToLower();
            if (!string.IsNullOrEmpty(forbiddenUploads))
            {
                var forbiddenFileExtensions = new ArrayList(Regex.Split(forbiddenUploads, "\\s+"));
                result = !forbiddenFileExtensions.Contains(fileExtension);
            }

            var allowedUploads = GetSetting("ALLOWED_UPLOADS").Trim().ToLower();
            if (!string.IsNullOrEmpty(allowedUploads))
            {
                var allowedFileExtensions = new ArrayList(Regex.Split(allowedUploads, "\\s+"));
                result = allowedFileExtensions.Contains(fileExtension);
            }

            return result;
        }

        /// <summary>
        /// Get the string to write to the response
        /// </summary>
        /// <param name="type">Type of the response</param>
        /// <param name="message">Additional message</param>
        /// <returns>String to write to the response</returns>
        protected virtual ResponseMessage GetResponse(string type, string message)
        {
            return new ResponseMessage()
            {
                Response = type,
                Message = message?.Replace("\"", "\\\"")
            };
        }

        /// <summary>
        /// Get the string to write a success response
        /// </summary>
        /// <param name="message">Additional message</param>
        /// <returns>String to write to the response</returns>
        protected virtual ResponseMessage GetSuccessResponse(string message = null)
        {
            return GetResponse("ok", message);
        }

        /// <summary>
        /// Get the string to write an error response
        /// </summary>
        /// <param name="message">Additional message</param>
        /// <returns>String to write to the response</returns>
        protected virtual ResponseMessage GetErrorResponse(string message = null)
        {
            return GetResponse("error", message);
        }

        /// <summary>
        /// Get all available directories as a directory tree
        /// </summary>
        /// <param name="type">Type of the file</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> ListDirTree(string type)
        {
            var folders = new List<DirListModel>();
            var allDirectories = _mediaFileStore.GetDirectoryContent("uploaded", includeSubDirectories: true, listFiles: false);

            var defaultDirectory = await _mediaFileStore.GetPhysicalDirectoryInfo("uploaded");
            var inforootFiles = _mediaFileStore.GetDirectoryContent("uploaded", includeSubDirectories: false, listDirectories: false);

            folders.Add(new DirListModel()
            {
                FileCount = GetFiles(inforootFiles, type).Count.ToString(),
                FolderCount = Directory.GetDirectories(defaultDirectory.PhysicalPath).Length.ToString(),
                Folder = "uploaded",
            });

            foreach (var item in allDirectories)
            {
                var infoDir = _mediaFileStore.GetDirectoryContent(item.Path, includeSubDirectories: false, listFiles: false);
                var infoFiles = _mediaFileStore.GetDirectoryContent(item.Path, includeSubDirectories: true, listDirectories: false);
                folders.Add(new DirListModel()
                {
                    Folder = $"{item.Path}",
                    FolderCount = infoDir.Count.ToString(),
                    FileCount = GetFiles(infoFiles, type).Count.ToString()
                });

            }
            return Ok(folders);
        }

        /// <summary>
        /// Get files in the passed directory
        /// </summary>
        /// <param name="directoryPath">Path to the files directory</param>
        /// <param name="type">Type of the files</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> ListFiles(string directoryPath, string type)
        {
            
            var infoFiles = _mediaFileStore.GetDirectoryContent(directoryPath, includeSubDirectories: false, listDirectories: false, listFiles: true);

            var files = new List<FileListModel>();
            foreach (var item in infoFiles)
            {
                if (!item.IsDirectory)
                {
                    var filemodel = new FileListModel();
                    filemodel.LastWriteTime = Math.Ceiling(GetTimestamp(item.LastModifiedUtc)).ToString();
                    filemodel.Length = item.Length.ToString();
                    filemodel.Path = item.Path;
                    if (GetFileType(new FileInfo(item.Name).Extension) == "image")
                    {
                        try
                        {
                            using var stream = await _mediaFileStore.GetFileStream(item);
                            using var image = SKBitmap.Decode(stream);
                            filemodel.Width = image.Width.ToString();
                            filemodel.Height = image.Height.ToString();
                        }
                        catch
                        {
                            continue;
                        }

                    }
                    if (IsFileType(item, type))
                        files.Add(filemodel);
                }
            }
            return Ok(files);
        }

        /// <summary>
        /// Get files in the passed directory
        /// </summary>
        /// <param name="directoryPath">Path to the files directory</param>
        /// <param name="type">Type of the files</param>
        /// <returns>List of paths to the files</returns>
        private List<string> GetFiles(IList<IFileStoreEntry> fileStoreEntries, string type)
        {
            if (type == "#")
                type = string.Empty;

            var files = new List<string>();
            foreach (var fileName in fileStoreEntries)
            {
                if (!fileName.IsDirectory)
                    if (string.IsNullOrEmpty(type) || GetFileType(new FileInfo(fileName.Name).Extension) == type)
                        files.Add(fileName.Name);
            }

            return files;
        }

        private bool IsFileType(IFileStoreEntry file, string type)
        {
            if (type == "#")
                type = string.Empty;

            type = type.TrimEnd('#');

            if (!file.IsDirectory)
                if (string.IsNullOrEmpty(type) || GetFileType(new FileInfo(file.Name).Extension) == type)
                    return true;

            return false;
        }

        /// <summary>
        /// Get the Unix timestamp by passed date
        /// </summary>
        /// <param name="date">Date and time</param>
        /// <returns>Unix timestamp</returns>
        private double GetTimestamp(DateTime date)
        {
            return (date.ToLocalTime() - new DateTime(1970, 1, 1, 0, 0, 0).ToLocalTime()).TotalSeconds;
        }



        /// <summary>
        /// Copy the file
        /// </summary>
        /// <param name="sourcePath">Path to the source file</param>
        /// <param name="destinationPath">Path to the destination file</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> CopyFile(string sourcePath, string destinationPath)
        {
            try
            {
                await _mediaFileStore.CopyFile(sourcePath, destinationPath);
                return Ok(GetSuccessResponse());
            }
            catch
            {
                throw new Exception(GetLanguageResource("E_CopyFile"));
            }
        }

        /// <summary>
        /// Create the new directory
        /// </summary>
        /// <param name="parentDirectoryPath">Path to the parent directory</param>
        /// <param name="name">Name of the new directory</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> CreateDir(string parentDirectoryPath, string name)
        {
            var created = _mediaFileStore.TryCreateDirectory($"{parentDirectoryPath}/{name}");
            if (created)
                return await Task.FromResult(Ok(GetSuccessResponse()));
            else
                throw new Exception(GetLanguageResource("E_CopyFileInvalisPath"));

        }
        protected virtual async Task<IActionResult> DeleteDir(string path)
        {
            var deleted = await _mediaFileStore.TryDeleteDirectory(path);
            if (deleted)
                return Ok(GetSuccessResponse());
            else
                throw new Exception(GetLanguageResource("E_CannotDeleteDir"));

        }

        /// <summary>
        /// Delete the file
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> DeleteFile(string path)
        {
            var deleted = await _mediaFileStore.TryDeleteFile(path);
            if (deleted)
                return Ok(GetSuccessResponse());
            else
                throw new Exception(GetLanguageResource("E_DeletеFile"));

        }

        /// <summary>
        /// Download the file from the server
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> DownloadFile(string path)
        {
            var infoFiles = await _mediaFileStore.GetFileInfo(path);

            var memory = new MemoryStream();
            using (var stream = await _mediaFileStore.GetFileStream(infoFiles))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, GetContentType(infoFiles.Name), infoFiles.Name);
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats  " +  "officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
        }

        /// <summary>
        /// Move the file
        /// </summary>
        /// <param name="sourcePath">Path to the source file</param>
        /// <param name="destinationPath">Path to the destination file</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> MoveFile(string sourcePath, string destinationPath)
        {
            await _mediaFileStore.MoveFile(sourcePath, destinationPath);
            return Ok(GetSuccessResponse());
        }

        /// <summary>
        /// Rename the directory
        /// </summary>
        /// <param name="sourcePath">Path to the source directory</param>
        /// <param name="newName">New name of the directory</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> RenameDir(string sourcePath, string newName)
        {
            var rename = await _mediaFileStore.TryRenameDirectory(sourcePath, newName);
            if (rename)
                return Ok(GetSuccessResponse());
            else
                throw new Exception($"{GetLanguageResource("E_RenameDir")} \"{sourcePath}\"");

        }

        /// <summary>
        /// Rename the file
        /// </summary>
        /// <param name="sourcePath">Path to the source file</param>
        /// <param name="newName">New name of the file</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> RenameFile(string sourcePath, string newName)
        {
            await _mediaFileStore.RenameFile(sourcePath, newName);

            return Ok(GetSuccessResponse());

        }

        /// <summary>
        /// Create the thumbnail of the image and write it to the response
        /// </summary>
        /// <param name="path">Path to the image</param>
        protected virtual async Task<IActionResult> CreateThumbnail(string path)
        {
            try
            {
                var fileInfo = await _mediaFileStore.GetFileInfo(path);
                using var stream = await _mediaFileStore.GetFileStream(fileInfo);

                using (var image = SKBitmap.Decode(stream))
                {
                    float width, height;
                    int targetSize = 120;
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
                    using (var resized = image.Resize(new SKImageInfo((int)width, (int)height), SKFilterQuality.None))
                    {
                        using (var image2 = SKImage.FromBitmap(resized))
                        {
                            return File(image2.Encode().ToArray(), "image/png");
                        }
                    }
                }

            }
            catch
            {
                throw new Exception($"{GetLanguageResource("E_Thumbnail")}");
            }
        }

        /// <summary>
        /// Whether the request is made with ajax 
        /// </summary>
        /// <returns>True or false</returns>
        private async Task<bool> IsAjaxRequest()
        {
            var form = await HttpContext.Request.ReadFormAsync();
            return form != null &&
                !StringValues.IsNullOrEmpty(form["method"]) &&
                form["method"] == "ajax";
        }

        /// <summary>
        /// Upload files to a directory on passed path
        /// </summary>
        /// <param name="directoryPath">Path to directory to upload files</param>
        /// <returns>A task that represents the completion of the operation</returns>
        protected virtual async Task<IActionResult> UploadFile()
        {
            var form = await HttpContext.Request.ReadFormAsync();
            string directoryPath = form["d"];

            var result = GetSuccessResponse();
            var hasErrors = false;
            try
            {
                for (var i = 0; i < form.Files.Count; i++)
                {
                    var fileName = form.Files[i].FileName;
                    if (CanAllowedFile(fileName))
                    {
                        var mediaFilePath = _mediaFileStore.Combine(directoryPath, fileName);
                        using var stream = form.Files[i].OpenReadStream();
                        mediaFilePath = await _mediaFileStore.CreateFileFromStream(mediaFilePath, stream);
                    }
                    else
                    {
                        hasErrors = true;
                        result = GetErrorResponse(GetLanguageResource("E_UploadNotAll"));
                    }
                }
            }
            catch (Exception ex)
            {
                result = GetErrorResponse(ex.Message);
            }
            if (await IsAjaxRequest())
            {
                if (hasErrors)
                    result = GetErrorResponse(GetLanguageResource("E_UploadNotAll"));
            }
            return Ok(result);
        }

        #endregion
    }
}