using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Models.Plugins;
using Grand.Web.Common.Audit;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Plugins)]
public class PluginController(
    IRequestTranslationService translationService,
    ILogger<PluginController> logger,
    IHostApplicationLifetime applicationLifetime,
    IContextAccessor contextAccessor,
    IServiceProvider serviceProvider,
    IEnumTranslationService enumTranslationService,
    IWebHostEnvironment webHostEnvironment,
    ExtensionsConfig extConfig,
    IAuditEventRecorder auditEventRecorder)
    : BaseAdminController
{
    #region Utilities

    private readonly string PluginsPath = Path.Combine(webHostEnvironment.ContentRootPath, CommonPath.Plugins);

    [NonAction]
    protected virtual PluginModel PreparePluginModel(PluginInfo PluginInfo)
    {
        var pluginModel = PluginInfo.ToModel();
        //logo
        pluginModel.LogoUrl = PluginInfo.GetLogoUrl(Request.PathBase);

        //configuration URLs - a plugin that failed to load has no instance, its LoadError is shown instead
        if (PluginInfo.IsActive)
        {
            var pluginInstance = PluginInfo.Instance(serviceProvider);
            pluginModel.ConfigurationUrl = pluginInstance.ConfigurationUrl();
        }

        return pluginModel;
    }

    /// <summary>
    ///     Depth-first recursive delete, with handling for descendant directories open in Windows Explorer.
    /// </summary>
    /// <param name="path">Directory path</param>
    protected void DeleteDirectory(string path)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(path);

        // Ensure the path is within the PluginsPath
        if (!path.StartsWith(PluginsPath, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Attempt to delete a directory outside of the plugins path.");

        foreach (var directory in Directory.GetDirectories(path))
            DeleteDirectory(directory);

        if (Directory.Exists(path))
            Directory.Delete(path, true);

    }

    #endregion

    #region Methods

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        var model = new PluginListModel {
            //load modes
            AvailableLoadModes = enumTranslationService.ToSelectList(LoadPluginsStatus.All, false).ToList()
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult ListSelect(DataSourceRequest command, PluginListModel model)
    {
        var pluginInfos = PluginManager.ReferencedPlugins.ToList();
        var loadMode = (LoadPluginsStatus)model.SearchLoadModeId;
        switch (loadMode)
        {
            case LoadPluginsStatus.InstalledOnly:
                pluginInfos = pluginInfos.Where(x => x.Installed).ToList();
                break;
            case LoadPluginsStatus.NotInstalledOnly:
                pluginInfos = pluginInfos.Where(x => !x.Installed).ToList();
                break;
        }

        var items = new List<PluginModel>();
        foreach (var item in pluginInfos.OrderBy(x => x.Group)) items.Add(PreparePluginModel(item));

        var gridModel = new DataSourceResult {
            Data = items,
            Total = pluginInfos.Count
        };
        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> Install(string systemName)
    {
        try
        {
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
            var plugin = pluginInfo.Instance<IPlugin>(serviceProvider);
            await plugin.Install();
            await auditEventRecorder.Plugin(systemName, "installed");

            Success(translationService.GetResource("Admin.Plugins.Installed"));

            logger.LogInformation("The plugin has been installed by the user {CurrentCustomerEmail}",
                contextAccessor.WorkContext.CurrentCustomer.Email);

            //stop application
            applicationLifetime.StopApplication();
        }
        catch (Exception exc)
        {
            Error(exc);
        }

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> Uninstall(string systemName)
    {
        try
        {
            var pluginInfo = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == systemName);
            if (pluginInfo == null)
                //No plugin found with the specified id
                return RedirectToAction("List");

            //check whether plugin is installed
            if (!pluginInfo.Installed)
                return RedirectToAction("List");

            //uninstall plugin
            var plugin = pluginInfo.Instance<IPlugin>(serviceProvider);
            await plugin.Uninstall();
            await auditEventRecorder.Plugin(systemName, "uninstalled");

            Success(translationService.GetResource("Admin.Plugins.Uninstalled"));

            logger.LogInformation("The plugin has been uninstalled by the user {CurrentCustomerEmail}",
                contextAccessor.WorkContext.CurrentCustomer.Email);

            //stop application
            applicationLifetime.StopApplication();
        }
        catch (Exception exc)
        {
            Error(exc);
        }

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(string systemName)
    {
        if (extConfig.DisableUploadExtensions)
        {
            Error("Upload plugins is disable");
            return RedirectToAction("List");
        }

        try
        {
            var pluginInfo = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == systemName);
            if (pluginInfo == null)
                //No plugin found with the specified id
                return RedirectToAction("List");

            DeleteDirectory(pluginInfo.OriginalAssemblyFile.DirectoryName);
            await auditEventRecorder.Plugin(systemName, "removed");

            //uninstall plugin
            Success(translationService.GetResource("Admin.Plugins.Removed"));

            logger.LogInformation("The plugin has been removed by the user {CurrentCustomerEmail}",
                contextAccessor.WorkContext.CurrentCustomer.Email);

            //stop application
            applicationLifetime.StopApplication();
        }
        catch (Exception exc)
        {
            Error(exc);
        }

        return RedirectToAction("List");
    }

    public IActionResult ReloadList()
    {
        logger.LogInformation("Reload list of plugins by the user {CurrentCustomerEmail}",
            contextAccessor.WorkContext.CurrentCustomer.Email);

        //stop application
        applicationLifetime.StopApplication();
        return RedirectToAction("List");
    }


    [HttpPost]
    public async Task<IActionResult> UploadPlugin(IFormFile zippedFile)
    {
        if (extConfig.DisableUploadExtensions)
        {
            Error("Upload plugins is disable");
            return RedirectToAction("List");
        }

        if (zippedFile == null || zippedFile.Length == 0)
        {
            Error(translationService.GetResource("Admin.Common.UploadFile"));
            return RedirectToAction("List");
        }
        var tempDirectory = Path.Combine(webHostEnvironment.ContentRootPath, CommonPath.Plugins, CommonPath.TmpUploadPath);
        var zipFilePath = "";
        try
        {
            if (!Path.GetExtension(zippedFile.FileName).Equals(".zip", StringComparison.InvariantCultureIgnoreCase))
                throw new Exception("Only zip archives are supported");

            // Ensure that temp directory is created
            Directory.CreateDirectory(new DirectoryInfo(tempDirectory).FullName);

            // Generate a unique file name for the uploaded file
            var uniqueFileName = Guid.NewGuid().ToString() + ".zip";
            zipFilePath = Path.Combine(tempDirectory, uniqueFileName);

            // Copy original archive to the temp directory
            using (var fileStream = new FileStream(zipFilePath, FileMode.Create))
            {
                zippedFile.CopyTo(fileStream);
            }

            var installer = new PluginArchiveInstaller(PluginsPath, logger);
            var result = installer.Install(zipFilePath);
            await auditEventRecorder.Plugin(result.Manifest.SystemName, "uploaded");

            var message = translationService.GetResource("Admin.Plugins.Uploaded");
            Success(message);
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
        finally
        {
            // Delete temporary file
            DeleteTempDirectory(tempDirectory);
        }

        logger.LogInformation("The plugin has been uploaded by the user {CurrentCustomerEmail}",
            contextAccessor.WorkContext.CurrentCustomer.Email);

        //stop application
        applicationLifetime.StopApplication();

        return RedirectToAction("List");
    }

    private void DeleteTempDirectory(string tempDirectory)
    {
        try
        {
            DeleteDirectory(tempDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            //the previous plugin version moved here can hold an assembly this process has loaded;
            //it is not loaded from here on the next start and the next upload removes it
            logger.LogWarning(ex, "The temporary upload folder {TempDirectory} could not be deleted", tempDirectory);
        }
    }

    #endregion
}