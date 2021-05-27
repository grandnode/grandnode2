using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.System.Commands.Models.Security;
using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using Grand.Web.Models.Install;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class InstallController : Controller
    {
        #region Fields

        private readonly ICacheBase _cacheBase;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        public InstallController(
            ICacheBase cacheBase,
            IHostApplicationLifetime applicationLifetime,
            IServiceProvider serviceProvider,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _applicationLifetime = applicationLifetime;
            _serviceProvider = serviceProvider;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        public virtual async Task<IActionResult> Index()
        {
            if (DataSettingsManager.DatabaseIsInstalled())
                return RedirectToRoute("HomePage");

            var locService = _serviceProvider.GetRequiredService<IInstallationLocalizedService>();

            var installed = await _cacheBase.GetAsync("Installed", async () => { return await Task.FromResult(false); });
            if (installed)
                return View(new InstallModel() { Installed = true });

            var model = new InstallModel {
                AdminEmail = "admin@yourstore.com",
                InstallSampleData = false,
                DatabaseConnectionString = "",
            };

            model.AvailableProviders = Enum.GetValues(typeof(DbProvider)).Cast<DbProvider>().Select(v => new SelectListItem {
                Text = v.ToString(),
                Value = ((int)v).ToString()
            }).ToList();

            foreach (var lang in locService.GetAvailableLanguages())
            {
                model.AvailableLanguages.Add(new SelectListItem {
                    Value = Url.RouteUrl("InstallChangeLanguage", new { language = lang.Code }),
                    Text = lang.Name,
                    Selected = locService.GetCurrentLanguage().Code == lang.Code,
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
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Index(InstallModel model)
        {
            if (DataSettingsManager.DatabaseIsInstalled())
                return RedirectToRoute("HomePage");

            var locService = _serviceProvider.GetRequiredService<IInstallationLocalizedService>();

            if (model.DatabaseConnectionString != null)
                model.DatabaseConnectionString = model.DatabaseConnectionString.Trim();

            string connectionString = "";

            if (model.ConnectionInfo)
            {
                if (String.IsNullOrEmpty(model.DatabaseConnectionString))
                {
                    ModelState.AddModelError("", locService.GetResource("ConnectionStringRequired"));
                }
                else
                {
                    connectionString = model.DatabaseConnectionString;
                }
            }
            else
            {
                if (String.IsNullOrEmpty(model.MongoDBDatabaseName))
                {
                    ModelState.AddModelError("", locService.GetResource("DatabaseNameRequired"));
                }
                if (String.IsNullOrEmpty(model.MongoDBServerName))
                {
                    ModelState.AddModelError("", locService.GetResource("MongoDBServerNameRequired"));
                }
                string userNameandPassword = "";
                if (!(String.IsNullOrEmpty(model.MongoDBUsername)))
                {
                    userNameandPassword = model.MongoDBUsername + ":" + model.MongoDBPassword + "@";
                }

                connectionString = "mongodb://" + userNameandPassword + model.MongoDBServerName + "/" + model.MongoDBDatabaseName;
            }

            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    var mdb = new MongoDBContext();
                    if (await mdb.DatabaseExist(connectionString))
                        ModelState.AddModelError("", locService.GetResource("AlreadyInstalled"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                }
            }
            else
                ModelState.AddModelError("", locService.GetResource("ConnectionStringRequired"));

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

                    var installationService = _serviceProvider.GetRequiredService<IInstallationService>();
                    await installationService.InstallData(model.AdminEmail, model.AdminPassword, model.Collation, 
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
                    var permissionProviders = new List<Type>();
                    permissionProviders.Add(typeof(PermissionProvider));
                    foreach (var providerType in permissionProviders)
                    {
                        var provider = (IPermissionProvider)Activator.CreateInstance(providerType);
                        await _mediator.Send(new InstallPermissionsCommand() { PermissionProvider = provider });
                    }

                    //restart application
                    await _cacheBase.SetAsync("Installed", true, 120);
                    return View(new InstallModel() { Installed = true });
                }
                catch (Exception exception)
                {
                    //reset cache
                    DataSettingsManager.ResetCache();
                    await _cacheBase.Clear();

                    System.IO.File.Delete(CommonPath.SettingsPath);

                    ModelState.AddModelError("", string.Format(locService.GetResource("SetupFailed"), exception.Message + " " + exception.InnerException?.Message));
                }
            }
            //prepare db providers
            model.AvailableProviders = Enum.GetValues(typeof(DbProvider)).Cast<DbProvider>().Select(v => new SelectListItem {
                Text = v.ToString(),
                Value = ((int)v).ToString()
            }).ToList();

            //prepare language list
            foreach (var lang in locService.GetAvailableLanguages())
            {
                model.AvailableLanguages.Add(new SelectListItem {
                    Value = Url.RouteUrl("InstallChangeLanguage", new { language = lang.Code }),
                    Text = lang.Name,
                    Selected = locService.GetCurrentLanguage().Code == lang.Code,
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

            return View(model);
        }

        public virtual IActionResult ChangeLanguage(string language)
        {
            if (DataSettingsManager.DatabaseIsInstalled())
                return RedirectToRoute("HomePage");

            var locService = _serviceProvider.GetRequiredService<IInstallationLocalizedService>();
            locService.SaveCurrentLanguage(language);

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
