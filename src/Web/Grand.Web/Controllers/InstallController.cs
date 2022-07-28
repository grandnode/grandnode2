using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Commands.System.Security;
using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Infrastructure.Migrations;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using Grand.Web.Models.Install;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grand.Web.Controllers
{
    public partial class InstallController : Controller
    {
        #region Fields

        private readonly ICacheBase _cacheBase;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IMediator _mediator;
        private readonly DatabaseConfig _dbConfig;

        /// <summary>
        /// Cookie name to language for the installation page
        /// </summary>
        private const string LANGUAGE_COOKIE_NAME = ".Grand.installation.lang";

        #endregion

        #region Ctor

        public InstallController(
            ICacheBase cacheBase,
            IHostApplicationLifetime applicationLifetime,
            IServiceProvider serviceProvider,
            IMediator mediator,
            DatabaseConfig litedbConfig)
        {
            _cacheBase = cacheBase;
            _applicationLifetime = applicationLifetime;
            _serviceProvider = serviceProvider;
            _mediator = mediator;
            _dbConfig = litedbConfig;
        }

        #endregion

        #region Methods

        protected virtual string GetLanguage()
        {
            HttpContext.Request.Cookies.TryGetValue(LANGUAGE_COOKIE_NAME, out var language);

            if (string.IsNullOrEmpty(language))
            {
                //find by current browser culture
                if (HttpContext.Request.Headers.TryGetValue("Accept-Language", out var userLanguages))
                {
                    var userLanguage = userLanguages.FirstOrDefault().Return(l => l.Split(',')[0], string.Empty);
                    if (!string.IsNullOrEmpty(userLanguage))
                    {
                        return userLanguage;
                    }
                }
            }

            return language;
        }
        protected InstallModel PrepareModel(InstallModel model)
        {
            var locService = _serviceProvider.GetRequiredService<IInstallationLocalizedService>();

            model ??= new InstallModel {
                AdminEmail = "admin@yourstore.com",
                InstallSampleData = false,
                DatabaseConnectionString = "",
            };

            model.AvailableProviders = Enum.GetValues(typeof(DbProvider)).Cast<DbProvider>().Select(v => new SelectListItem {
                Text = v.ToString(),
                Value = ((int)v).ToString()
            }).ToList();

            model.SelectedLanguage = GetLanguage();

            foreach (var lang in locService.GetAvailableLanguages())
            {
                var selected = false;
                if (locService.GetCurrentLanguage(model.SelectedLanguage).Code == lang.Code)
                {
                    selected = true;
                    model.SelectedLanguage = lang.Code;
                }

                model.AvailableLanguages.Add(new SelectListItem {
                    Value = Url.RouteUrl("InstallChangeLanguage", new { language = lang.Code }),
                    Text = lang.Name,
                    Selected = selected,
                });
            }
            //prepare collation list
            foreach (var col in locService.GetAvailableCollations())
            {
                model.AvailableCollation.Add(new SelectListItem {
                    Value = col.Value,
                    Text = col.Name,
                    Selected = locService.GetCurrentLanguage().Code == col.Value,
                });
            }

            return model;
        }

        public virtual async Task<IActionResult> Index()
        {
            if (DataSettingsManager.DatabaseIsInstalled())
                return RedirectToRoute("HomePage");

            var locService = _serviceProvider.GetRequiredService<IInstallationLocalizedService>();

            var installed = await _cacheBase.GetAsync("Installed", async () => { return await Task.FromResult(false); });
            if (installed)
                return View(new InstallModel() { Installed = true });

            return View(PrepareModel(null));
        }

        protected string BuildConnectionString(IInstallationLocalizedService locService, InstallModel model)
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
                        ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "DatabaseNameRequired"));
                    if (string.IsNullOrEmpty(model.MongoDBServerName))
                        ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "MongoDBServerNameRequired"));

                    var userNameandPassword = "";
                    if (!(string.IsNullOrEmpty(model.MongoDBUsername)))
                        userNameandPassword = model.MongoDBUsername + ":" + model.MongoDBPassword + "@";

                    connectionString = "mongodb://" + userNameandPassword + model.MongoDBServerName + "/" + model.MongoDBDatabaseName;
                }
                else
                {
                    if (!_dbConfig.UseLiteDb)
                        ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "InfoLiteDb"));

                    if (string.IsNullOrEmpty(_dbConfig.LiteDbConnectionString))
                        ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "InfoLiteDbConnectionString"));

                    connectionString = _dbConfig.LiteDbConnectionString;
                }
            }
            return connectionString;
        }

        protected async Task CheckConnectionString(IInstallationLocalizedService locService, string connectionString, InstallModel model)
        {
            if (!_dbConfig.UseLiteDb && model.DataProvider == DbProvider.LiteDB)
            {
                ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "UseLiteDbEnable"));
            }
            if (_dbConfig.UseLiteDb && model.DataProvider != DbProvider.LiteDB)
            {
                ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "UseLiteDbDisable"));
            }

            if (ModelState.IsValid && !string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    if (model.DataProvider != DbProvider.LiteDB)
                    {
                        var mdb = _serviceProvider.GetRequiredService<IDatabaseContext>();
                        mdb.SetConnection(connectionString);
                        if (await mdb.DatabaseExist())
                            ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "AlreadyInstalled"));
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                }
            }
            else if(string.IsNullOrEmpty(connectionString)) ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "ConnectionStringRequired"));

        }

        [HttpPost]
        public virtual async Task<IActionResult> Index(InstallModel model)
        {
            if (DataSettingsManager.DatabaseIsInstalled())
                return RedirectToRoute("HomePage");

            var locService = _serviceProvider.GetRequiredService<IInstallationLocalizedService>();

            if (model.DatabaseConnectionString != null)
                model.DatabaseConnectionString = model.DatabaseConnectionString.Trim();

            var connectionString = BuildConnectionString(locService, model);

            await CheckConnectionString(locService, connectionString, model);

            if (ModelState.IsValid)
            {
                try
                {
                    //save settings
                    var settings = new DataSettings {
                        ConnectionString = connectionString,
                        DbProvider = model.DataProvider
                    };

                    await DataSettingsManager.SaveSettings(settings);
                    DataSettingsManager.LoadSettings(reloadSettings: true);

                    var installationService = _serviceProvider.GetRequiredService<IInstallationService>();
                    await installationService.InstallData(
                        HttpContext.Request.Scheme, HttpContext.Request.Host,
                        model.AdminEmail, model.AdminPassword, model.Collation,
                        model.InstallSampleData, model.CompanyName, model.CompanyAddress, model.CompanyPhoneNumber, model.CompanyEmail);

                    //reset cache
                    DataSettingsManager.ResetCache();

                    PluginManager.ClearPlugins();

                    var pluginsInfo = PluginManager.ReferencedPlugins.ToList();

                    foreach (var pluginInfo in pluginsInfo)
                    {
                        try
                        {
                            var plugin = pluginInfo.Instance<IPlugin>(_serviceProvider);
                            await plugin.Install();
                        }
                        catch (Exception ex)
                        {
                            var _logger = _serviceProvider.GetRequiredService<ILogger>();
                            await _logger.InsertLog(Domain.Logging.LogLevel.Error, "Error during installing plugin " + pluginInfo.SystemName,
                                ex.Message + " " + ex.InnerException?.Message);
                        }
                    }

                    //register default permissions
                    var permissionProvider = _serviceProvider.GetRequiredService<IPermissionProvider>();
                    await _mediator.Send(new InstallPermissionsCommand() { PermissionProvider = permissionProvider });

                    //install migration process - install only header
                    var migrationProcess = _serviceProvider.GetRequiredService<IMigrationProcess>();
                    migrationProcess.InstallApplication();

                    //restart application
                    await _cacheBase.GetAsync("Installed", () => Task.FromResult(true));
                    return View(new InstallModel() { Installed = true });
                }
                catch (Exception exception)
                {
                    //reset cache
                    DataSettingsManager.ResetCache();
                    await _cacheBase.Clear();

                    System.IO.File.Delete(CommonPath.SettingsPath);

                    ModelState.AddModelError("", string.Format(locService.GetResource(model.SelectedLanguage, "SetupFailed"), exception.Message + " " + exception.InnerException?.Message));
                }
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
}
