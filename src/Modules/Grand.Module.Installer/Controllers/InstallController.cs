using Grand.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Module.Installer.Interfaces;
using Grand.Module.Installer.Models;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Grand.Module.Installer.Controllers;

public class InstallController : Controller
{

    #region Fields

    private readonly ICacheBase _cacheBase;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly DatabaseConfig _dbConfig;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InstallController> _logger;

    /// <summary>
    ///     Cookie name to language for the installation page
    /// </summary>
    private const string LANGUAGE_COOKIE_NAME = ".Grand.installation.lang";

    #endregion

    #region Ctor

    public InstallController(
        ICacheBase cacheBase,
        IHostApplicationLifetime applicationLifetime,
        DatabaseConfig dbConfig,
        IConfiguration configuration,
        ILogger<InstallController> logger)
    {
        _cacheBase = cacheBase;
        _applicationLifetime = applicationLifetime;
        _dbConfig = dbConfig;
        _configuration = configuration;
        _logger = logger;
    }

    #endregion

    #region Methods

    protected virtual string? GetLanguage()
    {
        HttpContext.Request.Cookies.TryGetValue(LANGUAGE_COOKIE_NAME, out var language);

        if (!string.IsNullOrEmpty(language)) return language;
        //find by current browser culture
        if (!HttpContext.Request.Headers.TryGetValue("Accept-Language", out var userLanguages)) return language;
        var userLanguage = userLanguages.FirstOrDefault()?.Split(',')[0];
        return !string.IsNullOrEmpty(userLanguage) ? userLanguage : language;
    }

    private InstallModel PrepareModel(InstallModel? model)
    {
        var installationLocalizedService = HttpContext.RequestServices.GetRequiredService<IInstallationLocalizedService>();

        model ??= new InstallModel {
            AdminEmail = "admin@yourstore.com",
            InstallSampleData = false,
            AdminPassword = "",
            ConfirmPassword = ""
        };

        if (!string.IsNullOrEmpty(_configuration[SettingsConstants.ConnectionStrings]))
        {
            model.SkipConnection = true;
        }

        model.AvailableProviders = Enum.GetValues(typeof(DbProvider)).Cast<DbProvider>().Select(v => new SelectListItem {
            Text = v.ToString(),
            Value = ((int)v).ToString()
        }).ToList();

        model.SelectedLanguage = GetLanguage();

        foreach (var lang in installationLocalizedService.GetAvailableLanguages())
        {
            var selected = false;
            if (installationLocalizedService.GetCurrentLanguage(model.SelectedLanguage).Code == lang.Code)
            {
                selected = true;
                model.SelectedLanguage = lang.Code;
            }

            model.AvailableLanguages.Add(new SelectListItem {
                Value = Url.RouteUrl("InstallChangeLanguage", new { language = lang.Code }),
                Text = lang.Name,
                Selected = selected
            });
        }

        //prepare collation list
        foreach (var col in installationLocalizedService.GetAvailableCollations())
            model.AvailableCollation.Add(new SelectListItem {
                Value = col.Value,
                Text = col.Name,
                Selected = installationLocalizedService.GetCurrentLanguage().Code == col.Value
            });

        return model;
    }

    public virtual async Task<IActionResult> Index()
    {        
        var installed = await _cacheBase.GetAsync("Installed", async () => await Task.FromResult(false));
        return View(installed ? new InstallModel { Installed = true, AdminEmail = "", AdminPassword = "", ConfirmPassword = "" } : PrepareModel(null));
    }

    private string BuildConnectionString(IInstallationLocalizedService locService, InstallModel model)
    {
        var connectionString = "";

        if (model.ConnectionInfo && model.DataProvider != DbProvider.LiteDB)
        {
            if (string.IsNullOrEmpty(model.DatabaseConnectionString))
                ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "ConnectionStringRequired"));
            else
                connectionString = model.DatabaseConnectionString;
        }
        else
        {
            if (model.DataProvider != DbProvider.LiteDB)
            {
                if (string.IsNullOrEmpty(model.MongoDBDatabaseName))
                    ModelState.AddModelError("",
                        locService.GetResource(model.SelectedLanguage, "DatabaseNameRequired"));
                if (string.IsNullOrEmpty(model.MongoDBServerName))
                    ModelState.AddModelError("",
                        locService.GetResource(model.SelectedLanguage, "MongoDBServerNameRequired"));

                var builder = new MongoUrlBuilder {
                    Server = new MongoServerAddress(model.MongoDBServerName),
                    Username = model.MongoDBUsername,
                    Password = model.MongoDBPassword,
                    DatabaseName = model.MongoDBDatabaseName
                };
                connectionString = builder.ToString();
            }
            else
            {
                if (!_dbConfig.UseLiteDb)
                    ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "InfoLiteDb"));

                if (string.IsNullOrEmpty(_dbConfig.LiteDbConnectionString))
                    ModelState.AddModelError("",
                        locService.GetResource(model.SelectedLanguage, "InfoLiteDbConnectionString"));

                connectionString = _dbConfig.LiteDbConnectionString;
            }
        }

        return connectionString;
    }

    protected async Task CheckConnectionString(IInstallationLocalizedService locService, string connectionString,
        InstallModel model)
    {
        switch (_dbConfig.UseLiteDb)
        {
            case false when model.DataProvider == DbProvider.LiteDB:
                ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "UseLiteDbEnable"));
                break;
            case true when model.DataProvider != DbProvider.LiteDB:
                ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "UseLiteDbDisable"));
                break;
        }

        if (ModelState.IsValid && !string.IsNullOrEmpty(connectionString))
            try
            {
                if (model.DataProvider != DbProvider.LiteDB)
                {
                    var mdb = HttpContext.RequestServices.GetRequiredService<IDatabaseFactoryContext>().GetDatabaseContext(connectionString, DbProvider.MongoDB);
                    if (await mdb.DatabaseExist())
                        ModelState.AddModelError("",
                            locService.GetResource(model.SelectedLanguage, "AlreadyInstalled"));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        else if (string.IsNullOrEmpty(connectionString))
            ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "ConnectionStringRequired"));
    }
    
    [HttpPost]
    public virtual async Task<IActionResult> Index(InstallModel model)
    {
        var installed = await _cacheBase.GetAsync("Installed", async () => await Task.FromResult(false));
        if (installed)
            return View(new InstallModel { Installed = true, AdminEmail = "", AdminPassword = "", ConfirmPassword = "" });

        var locService = HttpContext.RequestServices.GetRequiredService<IInstallationLocalizedService>();

        if (model.DatabaseConnectionString != null)
            model.DatabaseConnectionString = model.DatabaseConnectionString.Trim();

        var connectionString = !string.IsNullOrEmpty(_configuration[SettingsConstants.ConnectionStrings]) ?
            _configuration[SettingsConstants.ConnectionStrings]:
            BuildConnectionString(locService, model);            

        await CheckConnectionString(locService, connectionString!, model);

        if (!ModelState.IsValid) return View(PrepareModel(model));
        try
        {
            //save settings
            var settings = new DataSettings {
                ConnectionString = connectionString,
                DbProvider = model.DataProvider
            };
            if (string.IsNullOrEmpty(_configuration[SettingsConstants.ConnectionStrings]))
            {
                await DataSettingsManager.Instance.SaveSettings(settings);
                DataSettingsManager.Instance.LoadSettings(true);
            }

            var installationService = HttpContext.RequestServices.GetRequiredService<IInstallationService>();
            await installationService.InstallData(
                HttpContext.Request.Scheme, HttpContext.Request.Host,
                model.AdminEmail, model.AdminPassword, model.Collation,
                model.InstallSampleData, model.CompanyName, model.CompanyAddress, model.CompanyPhoneNumber,
                model.CompanyEmail);

            _logger.LogInformation("Database has been installed");

            //reset cache
            DataSettingsManager.Instance.ResetCache();

            PluginManager.ClearPlugins();

            var pluginsInfo = PluginManager.ReferencedPlugins.ToList();

            foreach (var pluginInfo in pluginsInfo)
                try
                {
                    var plugin = pluginInfo.Instance<IPlugin>(HttpContext.RequestServices.CreateScope().ServiceProvider);
                    await plugin.Install();
                    _logger.LogInformation($"Plugin {plugin.PluginInfo.FriendlyName} has been installed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during installing plugin " + pluginInfo.SystemName,
                        ex.Message + " " + ex.InnerException?.Message);
                }

            //restart application
            await _cacheBase.SetAsync("Installed", async () => await Task.FromResult(true));
            return View(new InstallModel { Installed = true, AdminEmail = "", AdminPassword = "", ConfirmPassword = "" });
        }
        catch (Exception exception)
        {
            //reset cache
            DataSettingsManager.Instance.ResetCache();
            await _cacheBase.Clear();

            System.IO.File.Delete(Path.Combine(AppContext.BaseDirectory, CommonPath.AppData, CommonPath.SettingsFile));

            ModelState.AddModelError("",
                string.Format(locService.GetResource(model.SelectedLanguage!, "SetupFailed"),
                    exception.Message + " " + exception.InnerException?.Message));
            _logger.LogError(exception, exception.InnerException?.Message);
        }

        return View(PrepareModel(model));
    }

    public virtual IActionResult ChangeLanguage(string language)
    {
        if (DataSettingsManager.DatabaseIsInstalled())
            return RedirectToRoute("HomePage");

        var cookieOptions = new CookieOptions {
            Expires = DateTime.Now.AddHours(24),
            HttpOnly = true
        };

        HttpContext.Response.Cookies.Delete(LANGUAGE_COOKIE_NAME);
        HttpContext.Response.Cookies.Append(LANGUAGE_COOKIE_NAME, language, cookieOptions);

        //Reload the page
        return RedirectToAction("Index", "Install");
    }

    public virtual IActionResult RestartInstall()
    {
        if (DataSettingsManager.DatabaseIsInstalled())
            return RedirectToRoute("HomePage");

        //stop application
        _applicationLifetime.StopApplication();

        //Redirect to home page
        return RedirectToRoute("HomePage");
    }

    #endregion
}