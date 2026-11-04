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

        //configuration URLs only for an active plugin, which was loaded at startup: listing never loads one,
        //and a plugin that failed to load has no instance - its LoadError is shown instead
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

    /// <summary>
    ///     The plugin's changes waiting for the restart, matched by system name and by folder
    /// </summary>
    private static List<PluginPendingChange> PendingChangesOf(PluginInfo pluginInfo)
    {
        var folder = pluginInfo.OriginalAssemblyFile?.Directory?.Name;
        return PluginManager.GetPendingChanges()
            .Where(x => string.Equals(x.SystemName, pluginInfo.SystemName, StringComparison.OrdinalIgnoreCase)
                        || (folder != null && string.Equals(x.FolderName, folder, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    ///     Whether the plugin is installed now: an install or uninstall since the last restart already counts,
    ///     although the plugin list still shows the state the application was started with
    /// </summary>
    private static bool IsInstalled(PluginInfo pluginInfo, IEnumerable<PluginPendingChange> pending)
    {
        var change = pending.LastOrDefault(x =>
            x.Kind is PluginPendingChangeKind.Installed or PluginPendingChangeKind.Uninstalled);

        return change?.Kind switch {
            PluginPendingChangeKind.Installed => true,
            PluginPendingChangeKind.Uninstalled => false,
            _ => pluginInfo.Installed
        };
    }

    /// <summary>
    ///     The running code is not what is on disk any more (the folder was removed or replaced): installing or
    ///     uninstalling it would run the old plugin's code for the new one, or for one that is gone
    /// </summary>
    private bool RestartRequiredFirst(IEnumerable<PluginPendingChange> pending)
    {
        if (!pending.Any(x => x.Kind is PluginPendingChangeKind.Removed or PluginPendingChangeKind.Updated))
            return false;

        Error(translationService.GetResource("Admin.Plugins.RestartFirst"));
        return true;
    }

    /// <summary>
    ///     The installed plugins are set in configuration (Extensions:InstalledPlugins), not by the admin panel
    /// </summary>
    private bool InstalledPluginsManagedByConfiguration()
    {
        if (string.IsNullOrEmpty(extConfig.InstalledPlugins))
            return false;

        Error(translationService.GetResource("Admin.Plugins.ManagedByConfiguration"));
        return true;
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
        if (InstalledPluginsManagedByConfiguration())
            return RedirectToAction("List");

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

            var pending = PendingChangesOf(pluginInfo);
            if (RestartRequiredFirst(pending))
                return RedirectToAction("List");

            //check whether plugin is not installed
            if (IsInstalled(pluginInfo, pending))
                return RedirectToAction("List");

            //install plugin
            var plugin = pluginInfo.Instance<IPlugin>(serviceProvider);
            await plugin.Install();
            await auditEventRecorder.Plugin(systemName, "installed");

            Success(translationService.GetResource("Admin.Plugins.Installed.AfterRestart"));

            logger.LogInformation("The plugin has been installed by the user {CurrentCustomerEmail}",
                contextAccessor.WorkContext.CurrentCustomer.Email);
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
        if (InstalledPluginsManagedByConfiguration())
            return RedirectToAction("List");

        try
        {
            var pluginInfo = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == systemName);
            if (pluginInfo == null)
                //No plugin found with the specified id
                return RedirectToAction("List");

            var pending = PendingChangesOf(pluginInfo);
            if (RestartRequiredFirst(pending))
                return RedirectToAction("List");

            //check whether plugin is installed
            if (!IsInstalled(pluginInfo, pending))
                return RedirectToAction("List");

            //a plugin that cannot be loaded has no instance to clean up after itself: it is only marked
            //uninstalled, so it can be removed. An installed plugin was loaded at startup, one installed since
            //then by its install; loading here only covers one that is still listed without being loaded
            PluginManager.LoadForInstall(pluginInfo);
            if (pluginInfo.LoadError != null)
            {
                await Grand.Infrastructure.Plugins.PluginExtensions.MarkPluginAsUninstalled(pluginInfo.SystemName);
                //it never ran: the list shows it uninstalled, and removable, right away
                pluginInfo.Installed = false;
                await auditEventRecorder.Plugin(systemName, "uninstalled");

                Warning(translationService.GetResource("Admin.Plugins.Uninstalled.NotCleanedUp"));
            }
            else
            {
                var plugin = pluginInfo.Instance<IPlugin>(serviceProvider);
                await plugin.Uninstall();
                await auditEventRecorder.Plugin(systemName, "uninstalled");

                Success(translationService.GetResource("Admin.Plugins.Uninstalled.AfterRestart"));
            }

            logger.LogInformation("The plugin has been uninstalled by the user {CurrentCustomerEmail}",
                contextAccessor.WorkContext.CurrentCustomer.Email);
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

            //an installed plugin keeps its settings and data; it is uninstalled first
            if (IsInstalled(pluginInfo, PendingChangesOf(pluginInfo)))
            {
                Error(translationService.GetResource("Admin.Plugins.Remove.UninstallFirst"));
                return RedirectToAction("List");
            }

            DeleteDirectory(pluginInfo.OriginalAssemblyFile.DirectoryName);
            await auditEventRecorder.Plugin(systemName, "removed");

            Success(translationService.GetResource("Admin.Plugins.Removed.AfterRestart"));

            logger.LogInformation("The plugin has been removed by the user {CurrentCustomerEmail}",
                contextAccessor.WorkContext.CurrentCustomer.Email);
        }
        catch (Exception exc)
        {
            Error(exc);
        }

        return RedirectToAction("List");
    }

    /// <summary>
    ///     Restarts the application, which applies the plugin changes made since the last start. Under the
    ///     Plugins permission, so applying them does not take the System permission a general restart needs.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReloadList()
    {
        logger.LogInformation("Reload list of plugins by the user {CurrentCustomerEmail}",
            contextAccessor.WorkContext.CurrentCustomer.Email);
        await auditEventRecorder.Plugin("*", "restarted");

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

            var message = translationService.GetResource("Admin.Plugins.Uploaded.AfterRestart");
            Success(message);
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
        finally
        {
            //only the uploaded file is ours; the installer owns (and may keep) its per-install folders
            PluginArchiveInstaller.DeleteArchive(zipFilePath, logger);
        }

        logger.LogInformation("The plugin has been uploaded by the user {CurrentCustomerEmail}",
            contextAccessor.WorkContext.CurrentCustomer.Email);

        return RedirectToAction("List");
    }

    #endregion
}