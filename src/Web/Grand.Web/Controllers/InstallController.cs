﻿using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.System.Commands.Models.Security;
using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
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
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _applicationLifetime = applicationLifetime;
            _serviceProvider = serviceProvider;
            _mediator = mediator;
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

            if (model.ConnectionInfo)
            {
                if (string.IsNullOrEmpty(model.DatabaseConnectionString))
                    ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "ConnectionStringRequired"));
                else
                    connectionString = model.DatabaseConnectionString;
            }
            else
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
            return connectionString;
        }

        protected async Task CheckConnectionString(IInstallationLocalizedService locService, string connectionString, InstallModel model)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    var mdb = _serviceProvider.GetRequiredService<IDatabaseContext>();
                    mdb.SetConnection(connectionString);
                    if (await mdb.DatabaseExist())
                        ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "AlreadyInstalled"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                }
            }
            else
                ModelState.AddModelError("", locService.GetResource(model.SelectedLanguage, "ConnectionStringRequired"));

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
                    var permissionProviders = new List<Type>();
                    permissionProviders.Add(typeof(PermissionProvider));
                    foreach (var providerType in permissionProviders)
                    {
                        var provider = (IPermissionProvider)Activator.CreateInstance(providerType);
                        await _mediator.Send(new InstallPermissionsCommand() { PermissionProvider = provider });
                    }

                    //install migration process - install only header
                    var migrationProcess = _serviceProvider.GetRequiredService<IMigrationProcess>();
                    migrationProcess.InstallApplication();

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
