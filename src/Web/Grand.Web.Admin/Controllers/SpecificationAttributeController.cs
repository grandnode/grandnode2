using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.SpecificationAttributes)]
    public partial class SpecificationAttributeController : BaseAdminController
    {
        #region Fields

        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly SeoSettings _seoSettings;

        #endregion Fields

        #region Constructors

        public SpecificationAttributeController(ISpecificationAttributeService specificationAttributeService,
            ILanguageService languageService,
            ITranslationService translationService,
            ICustomerActivityService customerActivityService,
            IStoreService storeService,
            IWorkContext workContext,
            IGroupService groupService,
            SeoSettings seoSettings)
        {
            _specificationAttributeService = specificationAttributeService;
            _languageService = languageService;
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _storeService = storeService;
            _workContext = workContext;
            _groupService = groupService;
            _seoSettings = seoSettings;
        }

        #endregion

        #region Specification attributes

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var specificationAttributes = await _specificationAttributeService
                .GetSpecificationAttributes(command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = specificationAttributes.Select(x => x.ToModel()),
                Total = specificationAttributes.TotalCount
            };

            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new SpecificationAttributeModel();
            //locales
            await AddLocales(_languageService, model.Locales);

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create(SpecificationAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var specificationAttribute = model.ToEntity();
                specificationAttribute.SeName = SeoExtensions.GetSeName(string.IsNullOrEmpty(specificationAttribute.SeName) ? specificationAttribute.Name : specificationAttribute.SeName, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls, _seoSettings.SeoCharConversion);
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }
                await _specificationAttributeService.InsertSpecificationAttribute(specificationAttribute);
                //activity log
                await _customerActivityService.InsertActivity("AddNewSpecAttribute", specificationAttribute.Id, _translationService.GetResource("ActivityLog.AddNewSpecAttribute"), specificationAttribute.Name);
                Success(_translationService.GetResource("Admin.Catalog.Attributes.SpecificationAttributes.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = specificationAttribute.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var specificationAttribute = await _specificationAttributeService.GetSpecificationAttributeById(id);
            if (specificationAttribute == null)
                //No specification attribute found with the specified id
                return RedirectToAction("List");

            var model = specificationAttribute.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = specificationAttribute.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(SpecificationAttributeModel model, bool continueEditing)
        {
            var specificationAttribute = await _specificationAttributeService.GetSpecificationAttributeById(model.Id);
            if (specificationAttribute == null)
                //No specification attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                specificationAttribute = model.ToEntity(specificationAttribute);
                specificationAttribute.SeName = SeoExtensions.GetSeName(string.IsNullOrEmpty(specificationAttribute.SeName) ? specificationAttribute.Name : specificationAttribute.SeName, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls, _seoSettings.SeoCharConversion);
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }
                await _specificationAttributeService.UpdateSpecificationAttribute(specificationAttribute);
                //activity log
                await _customerActivityService.InsertActivity("EditSpecAttribute", specificationAttribute.Id, _translationService.GetResource("ActivityLog.EditSpecAttribute"), specificationAttribute.Name);

                Success(_translationService.GetResource("Admin.Catalog.Attributes.SpecificationAttributes.Updated"));

                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = specificationAttribute.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = specificationAttribute.GetTranslation(x => x.Name, languageId, false);
            });
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            var specificationAttribute = await _specificationAttributeService.GetSpecificationAttributeById(id);
            if (specificationAttribute == null)
                //No specification attribute found with the specified id
                return RedirectToAction("List");
            if (ModelState.IsValid)
            {
                await _specificationAttributeService.DeleteSpecificationAttribute(specificationAttribute);

                //activity log
                await _customerActivityService.InsertActivity("DeleteSpecAttribute", specificationAttribute.Id, _translationService.GetResource("ActivityLog.DeleteSpecAttribute"), specificationAttribute.Name);

                Success(_translationService.GetResource("Admin.Catalog.Attributes.SpecificationAttributes.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = specificationAttribute.Id });
        }

        #endregion

        #region Specification attribute options

        //list
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> OptionList(string specificationAttributeId, DataSourceRequest command)
        {
            var options = (await _specificationAttributeService.GetSpecificationAttributeById(specificationAttributeId)).SpecificationAttributeOptions.OrderBy(x => x.DisplayOrder);
            var gridModel = new DataSourceResult
            {
                Data = options.Select(x =>
                    {
                        var model = x.ToModel();
                        //in order to save performance to do not check whether a product is deleted, etc
                        model.NumberOfAssociatedProducts = _specificationAttributeService
                            .GetProductSpecificationAttributeCount("", x.Id);
                        return model;
                    }),
                Total = options.Count()
            };

            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> OptionCreatePopup(string specificationAttributeId)
        {
            var model = new SpecificationAttributeOptionModel
            {
                SpecificationAttributeId = specificationAttributeId
            };
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> OptionCreatePopup(SpecificationAttributeOptionModel model)
        {
            var specificationAttribute = await _specificationAttributeService.GetSpecificationAttributeById(model.SpecificationAttributeId);
            if (specificationAttribute == null)
                //No specification attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                var sao = model.ToEntity();
                sao.SeName = SeoExtensions.GetSeName(string.IsNullOrEmpty(sao.SeName) ? sao.Name : sao.SeName, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls, _seoSettings.SeoCharConversion);
                //clear "Color" values if it's disabled
                if (!model.EnableColorSquaresRgb)
                    sao.ColorSquaresRgb = null;

                specificationAttribute.SpecificationAttributeOptions.Add(sao);
                await _specificationAttributeService.UpdateSpecificationAttribute(specificationAttribute);

                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> OptionEditPopup(string id)
        {
            var sao = (await _specificationAttributeService.GetSpecificationAttributeByOptionId(id)).SpecificationAttributeOptions.Where(x => x.Id == id).FirstOrDefault();
            if (sao == null)
                //No specification attribute option found with the specified id
                return RedirectToAction("List");

            var model = sao.ToModel();
            model.EnableColorSquaresRgb = !String.IsNullOrEmpty(sao.ColorSquaresRgb);
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = sao.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> OptionEditPopup(SpecificationAttributeOptionModel model)
        {
            var specificationAttribute = await _specificationAttributeService.GetSpecificationAttributeByOptionId(model.Id);
            var sao = specificationAttribute.SpecificationAttributeOptions.Where(x => x.Id == model.Id).FirstOrDefault();
            if (sao == null)
                //No specification attribute option found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                sao = model.ToEntity(sao);
                sao.SeName = SeoExtensions.GetSeName(string.IsNullOrEmpty(sao.SeName) ? sao.Name : sao.SeName, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls, _seoSettings.SeoCharConversion);

                //clear "Color" values if it's disabled
                if (!model.EnableColorSquaresRgb)
                    sao.ColorSquaresRgb = null;

                await _specificationAttributeService.UpdateSpecificationAttribute(specificationAttribute);

                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> OptionDelete(string id)
        {
            if (ModelState.IsValid)
            {
                var specificationAttribute = await _specificationAttributeService.GetSpecificationAttributeByOptionId(id);
                var sao = specificationAttribute.SpecificationAttributeOptions.Where(x => x.Id == id).FirstOrDefault();
                if (sao == null)
                    throw new ArgumentException("No specification attribute option found with the specified id");

                await _specificationAttributeService.DeleteSpecificationAttributeOption(sao);

                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }
        #endregion
    }
}
