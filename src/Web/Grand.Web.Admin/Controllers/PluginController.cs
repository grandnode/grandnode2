using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Services.Security;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Plugins;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Plugins)]
    public partial class PluginController : BaseAdminController
    {
        #region Fields

        private readonly ITranslationService _translationService;
        private readonly IThemeProvider _themeProvider;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkContext _workContext;
        private readonly AppConfig _appConfig;
        #endregion

        #region Constructors

        public PluginController(
            ITranslationService translationService,
            IThemeProvider themeProvider,
            ILogger logger,
            IHostApplicationLifetime applicationLifetime,
            IWorkContext workContext,
            IServiceProvider serviceProvider,
            AppConfig appConfig)
        {
            _translationService = translationService;
            _themeProvider = themeProvider;
            _logger = logger;
            _workContext = workContext;
            _applicationLifetime = applicationLifetime;
            _serviceProvider = serviceProvider;
            _appConfig = appConfig;
        }

        #endregion

        #region Utilities

        [NonAction]
        protected virtual PluginModel PreparePluginModel(PluginInfo PluginInfo)
        {
            var pluginModel = PluginInfo.ToModel();
            //logo
            pluginModel.LogoUrl = PluginInfo.GetLogoUrl(_workContext);

            //configuration URLs
            if (PluginInfo.Installed)
            {
                var pluginInstance = PluginInfo.Instance(_serviceProvider);
                pluginModel.ConfigurationUrl = pluginInstance.ConfigurationUrl();
            }
            return pluginModel;
        }

        /// <summary>
        ///  Depth-first recursive delete, with handling for descendant directories open in Windows Explorer.
        /// </summary>
        /// <param name="path">Directory path</param>
        protected void DeleteDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            //find more info about directory deletion
            //and why we use this approach at https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true

            foreach (var directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }

        protected byte[] ToByteArray(Stream stream)
        {
            using (stream)
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    stream.CopyTo(memStream);
                    return memStream.ToArray();
                }
            }
        }
        #endregion

        #region Methods

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = new PluginListModel
            {
                //load modes
                AvailableLoadModes = LoadPluginsStatus.All.ToSelectList(HttpContext, false).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult ListSelect(DataSourceRequest command, PluginListModel model)
        {
            var pluginInfos = PluginManager.ReferencedPlugins.ToList();
            var loadMode = (LoadPluginsStatus)model.SearchLoadModeId;
            if (loadMode == LoadPluginsStatus.InstalledOnly)
                pluginInfos = pluginInfos.Where(x => x.Installed).ToList();
            if (loadMode == LoadPluginsStatus.NotInstalledOnly)
                pluginInfos = pluginInfos.Where(x => !x.Installed).ToList();

            var items = new List<PluginModel>();
            foreach (var item in pluginInfos.OrderBy(x => x.Group))
            {
                items.Add(PreparePluginModel(item));
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = pluginInfos.Count()
            };
            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> Install(IFormCollection form)
        {
            try
            {
                //get plugin system name
                string systemName = null;
                foreach (var formValue in form.Keys)
                    if (formValue.StartsWith("install-plugin-link-", StringComparison.OrdinalIgnoreCase))
                        systemName = formValue.Substring("install-plugin-link-".Length);

                var pluginInfo = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == systemName);
                if (pluginInfo == null)
                    //No plugin found with the specified id
                    return RedirectToAction("List");

                if (pluginInfo.SupportedVersion != GrandVersion.SupportedPluginVersion)
                {
                    Error("You can't install unsupported version of plugin");
                    return RedirectToAction("List");
                }
                //check whether plugin is not installed
                if (pluginInfo.Installed)
                    return RedirectToAction("List");

                //install plugin
                var plugin = pluginInfo.Instance<IPlugin>(_serviceProvider);
                await plugin.Install();

                Success(_translationService.GetResource("Admin.Plugins.Installed"));

                //stop application
                _applicationLifetime.StopApplication();
            }
            catch (Exception exc)
            {
                Error(exc);
            }

            return RedirectToAction("List");
        }

        [HttpPost]
        public async Task<IActionResult> Uninstall(IFormCollection form)
        {
            try
            {
                //get plugin system name
                string systemName = null;
                foreach (var formValue in form.Keys)
                    if (formValue.StartsWith("uninstall-plugin-link-", StringComparison.OrdinalIgnoreCase))
                        systemName = formValue.Substring("uninstall-plugin-link-".Length);

                var pluginInfo = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == systemName);
                if (pluginInfo == null)
                    //No plugin found with the specified id
                    return RedirectToAction("List");

                //check whether plugin is installed
                if (!pluginInfo.Installed)
                    return RedirectToAction("List");

                //uninstall plugin
                var plugin = pluginInfo.Instance<IPlugin>(_serviceProvider);
                await plugin.Uninstall();

                Success(_translationService.GetResource("Admin.Plugins.Uninstalled"));

                //stop application
                _applicationLifetime.StopApplication();
            }
            catch (Exception exc)
            {
                Error(exc);
            }

            return RedirectToAction("List");
        }

        [HttpPost]
        public IActionResult Remove(IFormCollection form)
        {
            if (_appConfig.DisableUploadExtensions)
            {
                Error("Upload plugins/themes is disable");
                return RedirectToAction("List");
            }

            try
            {
                //get plugin system name
                string systemName = null;
                foreach (var formValue in form.Keys)
                    if (formValue.StartsWith("remove-plugin-link-", StringComparison.OrdinalIgnoreCase))
                        systemName = formValue.Substring("remove-plugin-link-".Length);

                var pluginInfo = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == systemName);
                if (pluginInfo == null)
                    //No plugin found with the specified id
                    return RedirectToAction("List");

                var pluginsPath = CommonPath.PluginsPath;

                foreach (var folder in Directory.GetDirectories(pluginsPath))
                {
                    if (Path.GetFileName(folder) != "bin" && Directory.GetFiles(folder).Select(x => Path.GetFileName(x)).Contains(pluginInfo.PluginFileName))
                    {
                        DeleteDirectory(folder);
                    }
                }

                //uninstall plugin
                Success(_translationService.GetResource("Admin.Plugins.Removed"));

                //stop application
                _applicationLifetime.StopApplication();
            }
            catch (Exception exc)
            {
                Error(exc);
            }

            return RedirectToAction("List");
        }

        public IActionResult ReloadList()
        {
            //stop application
            _applicationLifetime.StopApplication();
            return RedirectToAction("List");
        }


        [HttpPost]
        public IActionResult UploadPlugin(IFormFile zippedFile)
        {
            if(_appConfig.DisableUploadExtensions)
            {
                Error("Upload plugins/themes is disable");
                return RedirectToAction("List");
            }
            if (zippedFile == null || zippedFile.Length == 0)
            {
                Error(_translationService.GetResource("Admin.Common.UploadFile"));
                return RedirectToAction("List");
            }

            string zipFilePath = "";
            try
            {
                if (!Path.GetExtension(zippedFile.FileName)?.Equals(".zip", StringComparison.InvariantCultureIgnoreCase) ?? true)
                    throw new Exception("Only zip archives are supported");

                //ensure that temp directory is created
                var tempDirectory = CommonPath.TmpUploadPath;
                Directory.CreateDirectory(new DirectoryInfo(tempDirectory).FullName);

                //copy original archive to the temp directory
                zipFilePath = Path.Combine(tempDirectory, zippedFile.FileName);
                using (var fileStream = new FileStream(zipFilePath, FileMode.Create))
                    zippedFile.CopyTo(fileStream);

                Upload(zipFilePath);

                var message = _translationService.GetResource("Admin.Plugins.Uploaded");
                Success(message);
            }
            finally
            {
                //delete temporary file
                if (!string.IsNullOrEmpty(zipFilePath))
                    System.IO.File.Delete(zipFilePath);
            }

            //stop application
            _applicationLifetime.StopApplication();

            return RedirectToAction("List");
        }

        [HttpPost]
        public IActionResult UploadTheme(IFormFile zippedFile)
        {
            if (_appConfig.DisableUploadExtensions)
            {
                Error("Upload plugins/themes is disable");
                return RedirectToAction("GeneralCommon", "Setting");
            }

            if (zippedFile == null || zippedFile.Length == 0)
            {
                Error(_translationService.GetResource("Admin.Common.UploadFile"));
                return RedirectToAction("GeneralCommon", "Setting");
            }

            string zipFilePath = "";

            try
            {
                if (!Path.GetExtension(zippedFile.FileName)?.Equals(".zip", StringComparison.InvariantCultureIgnoreCase) ?? true)
                    throw new Exception("Only zip archives are supported");

                //ensure that temp directory is created
                var tempDirectory = CommonPath.TmpUploadPath;
                Directory.CreateDirectory(new DirectoryInfo(tempDirectory).FullName);

                //copy original archive to the temp directory
                zipFilePath = Path.Combine(tempDirectory, zippedFile.FileName);
                using (var fileStream = new FileStream(zipFilePath, FileMode.Create))
                    zippedFile.CopyTo(fileStream);

                Upload(zipFilePath);

                var message = _translationService.GetResource("Admin.Configuration.Themes.Uploaded");
                Success(message);
            }
            catch (Exception ex)
            {
                var message = _translationService.GetResource("Admin.Configuration.Themes.Failed");
                Error(message + "\r\n" + ex.Message);
            }
            finally
            {
                //delete temporary file
                if (!string.IsNullOrEmpty(zipFilePath))
                    System.IO.File.Delete(zipFilePath);
            }

            return RedirectToAction("GeneralCommon", "Setting");
        }

        private void Upload(string archivePath)
        {
            var pluginsDirectory = CommonPath.PluginsPath;
            var themesDirectory = CommonPath.ThemePath;

            var uploadedItemDirectoryName = "";
            PluginInfo _pluginInfo = null;
            ThemeInfo _themeInfo = null;
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                var rootDirectories = archive.Entries.Where(entry => entry.FullName.Count(ch => ch == '/') == 1 && entry.FullName.EndsWith("/")).ToList();
                if (rootDirectories.Count != 1)
                {
                    throw new Exception($"The archive should contain only one root plugin or theme directory. " +
                        $"For example, Payments.PayPalDirect or DefaultClean. ");
                }

                //get directory name (remove the ending /)
                uploadedItemDirectoryName = rootDirectories.First().FullName.TrimEnd('/');

                var themeDescriptorEntry = archive.Entries.Where(x => x.FullName.Contains("theme.cfg")).FirstOrDefault();
                if (themeDescriptorEntry != null)
                {
                    using var unzippedEntryStream = themeDescriptorEntry.Open();
                    using var reader = new StreamReader(unzippedEntryStream);
                    _themeInfo = _themeProvider.GetThemeDescriptorFromText(reader.ReadToEnd());
                }
                else
                {
                    var supportedVersion = false;
                    var _fpath = "";
                    foreach (var entry in archive.Entries.Where(x => x.FullName.Contains(".dll")))
                    {
                        using (var unzippedEntryStream = entry.Open())
                        {
                            try
                            {
                                var assembly = Assembly.Load(ToByteArray(unzippedEntryStream));
                                var pluginInfo = assembly.GetCustomAttribute<PluginInfoAttribute>();
                                if (pluginInfo != null)
                                {
                                    if (pluginInfo.SupportedVersion == GrandVersion.SupportedPluginVersion)
                                    {
                                        supportedVersion = true;
                                        _fpath = entry.FullName.Substring(0, entry.FullName.LastIndexOf("/"));
                                        archive.Entries.Where(x => !x.FullName.Contains(_fpath)).ToList()
                                        .ForEach(y => { archive.GetEntry(y.FullName).Delete(); });

                                        _pluginInfo = new PluginInfo();
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex.Message);
                            };
                        }
                    }
                    if (!supportedVersion)
                        throw new Exception($"This plugin doesn't support the current version - {GrandVersion.SupportedPluginVersion}");
                    else
                    {
                        var pluginname = _fpath.Substring(_fpath.LastIndexOf('/') + 1);
                        var _path = "";

                        var entries = archive.Entries.ToArray();
                        foreach (var y in entries)
                        {
                            if (y.Name.Length > 0)
                                _path = y.FullName.Replace(y.Name, "").Replace(_fpath, pluginname).TrimEnd('/');
                            else
                                _path = y.FullName.Replace(_fpath, pluginname);

                            var _entry = archive.CreateEntry($"{_path}/{y.Name}");
                            using (var a = y.Open())
                            using (var b = _entry.Open())
                                a.CopyTo(b);

                            archive.GetEntry(y.FullName).Delete();

                        }
                    }
                }
            }

            if (_pluginInfo == null && _themeInfo == null)
                throw new Exception("No info file is found.");

            if (string.IsNullOrEmpty(uploadedItemDirectoryName))
                throw new Exception($"Cannot get the {(_pluginInfo != null ? "plugin" : "theme")} directory name");

            var directoryPath = _pluginInfo != null ? pluginsDirectory : themesDirectory;
            var pathToUpload = Path.Combine(directoryPath, uploadedItemDirectoryName);

            try
            {
                if (Directory.Exists(pathToUpload))
                    DeleteDirectory(pathToUpload);
            }
            catch { }

            ZipFile.ExtractToDirectory(archivePath, directoryPath);
        }

        #endregion
    }
}
