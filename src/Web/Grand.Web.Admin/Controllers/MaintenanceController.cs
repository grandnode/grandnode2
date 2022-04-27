using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Commands.System.Common;
using Grand.Domain.Media;
using Grand.Domain.Seo;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Directory;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Maintenance)]
    public partial class MaintenanceController : BaseAdminController
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly ISlugService _slugService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IRobotsTxtService _robotsTxtService;
        private readonly IMediator _mediator;
        private readonly IMediaFileStore _mediaFileStore;

        #endregion

        #region Constructors

        public MaintenanceController(
            ICustomerService customerService,
            ISlugService slugService,
            IDateTimeService dateTimeService,
            ILanguageService languageService,
            ITranslationService translationService,
            IRobotsTxtService robotsTxtService,
            IMediator mediator,
            IMediaFileStore mediaFileStore)
        {
            _customerService = customerService;
            _slugService = slugService;
            _dateTimeService = dateTimeService;
            _languageService = languageService;
            _translationService = translationService;
            _robotsTxtService = robotsTxtService;
            _mediaFileStore = mediaFileStore;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        public IActionResult Maintenance()
        {
            var model = new MaintenanceModel() {
                DeleteGuests = new MaintenanceModel.DeleteGuestsModel() {
                    EndDate = DateTime.UtcNow.AddDays(-7),
                    OnlyWithoutShoppingCart = true,
                },

            };

            if (TempData["NumberOfDeletedCustomers"] != null)
                model.DeleteGuests.NumberOfDeletedCustomers = (int)TempData["NumberOfDeletedCustomers"];

            if (TempData["DeleteActivityLog"] != null)
                model.DeleteActivityLog = (bool)TempData["DeleteActivityLog"];

            if (TempData["DeleteSystemLog"] != null)
                model.DeleteSystemLog = (bool)TempData["DeleteSystemLog"];

            if (TempData["NumberOfConvertItems"] != null)
            {
                model.ConvertedPictureModel = new MaintenanceModel.ConvertPictureModel {
                    NumberOfConvertItems = (int)TempData["NumberOfConvertItems"]
                };
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> MaintenanceDeleteGuests(MaintenanceModel model)
        {
            DateTime? startDateValue = (model.DeleteGuests.StartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.DeleteGuests.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.DeleteGuests.EndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.DeleteGuests.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            TempData["NumberOfDeletedCustomers"] = await _customerService.DeleteGuestCustomers(startDateValue, endDateValue, model.DeleteGuests.OnlyWithoutShoppingCart);

            return RedirectToAction("Maintenance");
        }

        [HttpPost]
        public async Task<IActionResult> MaintenanceClearMostViewed(MaintenanceModel model)
        {
            await _mediator.Send(new ClearMostViewedCommand());
            return RedirectToAction("Maintenance");
        }

        [HttpPost]
        public IActionResult MaintenanceDeleteFiles(MaintenanceModel model)
        {
            //TO DO
            return RedirectToAction("Maintenance");
        }


        [HttpPost]
        public async Task<IActionResult> MaintenanceDeleteActivitylog(MaintenanceModel model)
        {
            await _mediator.Send(new DeleteActivitylogCommand());
            TempData["DeleteActivityLog"] = true;
            return RedirectToAction("Maintenance");
        }

        [HttpPost]
        public async Task<IActionResult> MaintenanceDeleteSystemlog(MaintenanceModel model, [FromServices] ILogger logger)
        {
            await logger.ClearLog();
            TempData["DeleteSystemLog"] = true;
            return RedirectToAction("Maintenance");
        }


        [HttpPost]
        public async Task<IActionResult> MaintenanceConvertPicture(
            [FromServices] IPictureService pictureService,
            [FromServices] StorageSettings storageSettings,
            [FromServices] MediaSettings mediaSettings,
            [FromServices] ILogger logger)
        {
            var numberOfConvertItems = 0;
            if (storageSettings.PictureStoreInDb)
            {
                var pictures = pictureService.GetPictures();
                foreach (var picture in pictures)
                {
                    try
                    {
                        var pictureConverted = pictureService.ConvertPicture(picture.PictureBinary, mediaSettings.ImageQuality);
                        await pictureService.UpdatePicture(picture.Id, pictureConverted, "image/webp", picture.SeoFilename, picture.AltAttribute, picture.TitleAttribute, 
                            picture.Style, picture.ExtraField, true, false);
                        numberOfConvertItems += 1;
                    }
                    catch (Exception ex)
                    {
                        _ = logger.Error($"Error on converting picture with id {picture.Id} to webp format", ex);
                    }

                }
            }
            TempData["NumberOfConvertItems"] = numberOfConvertItems;
            return RedirectToAction("Maintenance");
        }

        public IActionResult SeNames()
        {
            var model = new UrlEntityListModel();
            //"Active" property
            //0 - all (according to "IsActive" parameter)
            //1 - active only
            //2 - inactive only
            model.AvailableActiveOptions.Add(new SelectListItem { Text = _translationService.GetResource("admin.configuration.senames.Search.All"), Value = "0" });
            model.AvailableActiveOptions.Add(new SelectListItem { Text = _translationService.GetResource("admin.configuration.senames.Search.ActiveOnly"), Value = "1" });
            model.AvailableActiveOptions.Add(new SelectListItem { Text = _translationService.GetResource("admin.configuration.senames.Search.InActiveOnly"), Value = "2" });

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SeNames(DataSourceRequest command, UrlEntityListModel model)
        {
            bool? active = null;
            switch (model.SearchActiveId)
            {
                case 1:
                    active = true;
                    break;
                case 2:
                    active = false;
                    break;
                default:
                    break;
            }
            var entityUrls = await _slugService.GetAllEntityUrl(model.SeName, active, command.Page - 1, command.PageSize);
            var items = new List<UrlEntityModel>();
            foreach (var x in entityUrls)
            {
                //language
                string languageName;
                if (String.IsNullOrEmpty(x.LanguageId))
                {
                    languageName = _translationService.GetResource("admin.configuration.senames.Language.Standard");
                }
                else
                {
                    var language = await _languageService.GetLanguageById(x.LanguageId);
                    languageName = language != null ? language.Name : "Unknown";
                }

                //details URL
                string detailsUrl = "";
                var entityName = x.EntityName != null ? x.EntityName.ToLowerInvariant() : "";
                switch (entityName)
                {
                    case "brand":
                        detailsUrl = Url.Action("Edit", "Brand", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "blogpost":
                        detailsUrl = Url.Action("Edit", "Blog", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "category":
                        detailsUrl = Url.Action("Edit", "Category", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "collection":
                        detailsUrl = Url.Action("Edit", "Collection", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "product":
                        detailsUrl = Url.Action("Edit", "Product", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "newsitem":
                        detailsUrl = Url.Action("Edit", "News", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "page":
                        detailsUrl = Url.Action("Edit", "Page", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "vendor":
                        detailsUrl = Url.Action("Edit", "Vendor", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "course":
                        detailsUrl = Url.Action("Edit", "Course", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "knowledgebasecategory":
                        detailsUrl = Url.Action("EditCategory", "Knowledgebase", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "knowledgebasearticle":
                        detailsUrl = Url.Action("EditArticle", "Knowledgebase", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    default:
                        break;
                }

                items.Add(new UrlEntityModel {
                    Id = x.Id,
                    Name = x.Slug,
                    EntityId = x.EntityId,
                    EntityName = x.EntityName,
                    IsActive = x.IsActive,
                    Language = languageName,
                    DetailsUrl = detailsUrl
                });

            }
            var gridModel = new DataSourceResult {
                Data = items,
                Total = entityUrls.TotalCount
            };
            return Json(gridModel);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteSelectedSeNames(ICollection<string> selectedIds)
        {
            if (selectedIds != null)
            {
                var entityUrls = new List<EntityUrl>();
                foreach (var id in selectedIds)
                {
                    var entityUrl = await _slugService.GetEntityUrlById(id);
                    if (entityUrl != null)
                        entityUrls.Add(entityUrl);
                }
                foreach (var entityUrl in entityUrls)
                    await _slugService.DeleteEntityUrl(entityUrl);
            }

            return Json(new { Result = true });
        }


        #endregion

        #region Custom css/js/robots.txt

        public async Task<IActionResult> CustomCss()
        {
            var model = new Editor();
            var pathFile = _mediaFileStore.Combine("assets", "custom", "style.css");
            var file = await _mediaFileStore.GetFileInfo(pathFile);
            if (file != null)
            {
                model.Content = await _mediaFileStore.ReadAllText(pathFile);
            }

            if (string.IsNullOrEmpty(model.Content))
                model.Content = "/* my custom style */";

            return View(model);
        }

        public async Task<IActionResult> CustomJs()
        {
            var model = new Editor();
            var pathFile = _mediaFileStore.Combine("assets", "custom", "script.js");
            var file = await _mediaFileStore.GetFileInfo(pathFile);
            if (file != null)
            {
                model.Content = await _mediaFileStore.ReadAllText(pathFile);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEditor(string content = "", bool css = true)
        {
            try
            {
                var path = _mediaFileStore.Combine("assets", "custom");
                _mediaFileStore.TryCreateDirectory(path);

                var pathFile = _mediaFileStore.Combine(path, css ? "style.css" : "script.js");
                await _mediaFileStore.WriteAllText(pathFile, content);

                return Json(_translationService.GetResource("Admin.Common.Content.Saved"));
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        public async Task<IActionResult> RobotsTxt()
        {
            var storeScope = await GetActiveStore();

            var model = new RobotsTxtModel();
            var robots = await _robotsTxtService.GetRobotsTxt(storeScope);
            if (robots != null)
            {
                model.Name = robots.Name;
                model.Text = robots.Text;
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RobotsTxt(RobotsTxtModel model)
        {
            if (ModelState.IsValid)
            {
                var storeScope = await GetActiveStore();

                var robotsTxt = await _robotsTxtService.GetRobotsTxt(storeScope);
                if (robotsTxt == null)
                {
                    await _robotsTxtService.InsertRobotsTxt(new Domain.Common.RobotsTxt() {
                        Name = model.Name,
                        Text = model.Text,
                        StoreId = storeScope
                    });
                }
                else
                {
                    robotsTxt.Text = model.Text;
                    robotsTxt.Name = model.Name;
                    await _robotsTxtService.UpdateRobotsTxt(robotsTxt);
                }

                Success(_translationService.GetResource("Admin.Common.Content.Saved"));
                return RedirectToAction("RobotsTxt");
            }

            return View(model);
        }

        #endregion
    }
}