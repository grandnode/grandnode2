using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.CheckoutAttributes)]
    public partial class CheckoutAttributeController : BaseAdminController
    {
        #region Fields

        private readonly ICheckoutAttributeService _checkoutAttributeService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IMeasureService _measureService;
        private readonly MeasureSettings _measureSettings;
        private readonly IStoreService _storeService;
        private readonly IGroupService _groupService;
        private readonly ICheckoutAttributeViewModelService _checkoutAttributeViewModelService;

        #endregion

        #region Constructors

        public CheckoutAttributeController(ICheckoutAttributeService checkoutAttributeService,
            ILanguageService languageService,
            ITranslationService translationService,
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            IMeasureService measureService,
            MeasureSettings measureSettings,
            IStoreService storeService,
            IGroupService groupService,
            ICheckoutAttributeViewModelService checkoutAttributeViewModelService)
        {
            _checkoutAttributeService = checkoutAttributeService;
            _languageService = languageService;
            _translationService = translationService;
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _measureService = measureService;
            _measureSettings = measureSettings;
            _storeService = storeService;
            _groupService = groupService;
            _checkoutAttributeViewModelService = checkoutAttributeViewModelService;
        }

        #endregion

        #region Checkout attributes

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var checkoutAttributes = await _checkoutAttributeViewModelService.PrepareCheckoutAttributeListModel();
            var gridModel = new DataSourceResult
            {
                Data = checkoutAttributes.ToList(),
                Total = checkoutAttributes.Count()
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = await _checkoutAttributeViewModelService.PrepareCheckoutAttributeModel();
            //locales
            await AddLocales(_languageService, model.Locales);

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create(CheckoutAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var checkoutAttribute = await _checkoutAttributeViewModelService.InsertCheckoutAttributeModel(model);
                Success(_translationService.GetResource("Admin.Orders.CheckoutAttributes.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = checkoutAttribute.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //tax categories
            await _checkoutAttributeViewModelService.PrepareTaxCategories(model, null, true);

            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(id);
            if (checkoutAttribute == null)
                //No checkout attribute found with the specified id
                return RedirectToAction("List");

            var model = checkoutAttribute.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = checkoutAttribute.GetTranslation(x => x.Name, languageId, false);
                locale.TextPrompt = checkoutAttribute.GetTranslation(x => x.TextPrompt, languageId, false);
            });

            //tax categories
            await _checkoutAttributeViewModelService.PrepareTaxCategories(model, checkoutAttribute, false);

            //condition
            await _checkoutAttributeViewModelService.PrepareConditionAttributes(model, checkoutAttribute);

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(CheckoutAttributeModel model, bool continueEditing)
        {
            var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(model.Id);
            if (checkoutAttribute == null)
                //No checkout attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                checkoutAttribute = await _checkoutAttributeViewModelService.UpdateCheckoutAttributeModel(checkoutAttribute, model);
                Success(_translationService.GetResource("Admin.Orders.CheckoutAttributes.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = checkoutAttribute.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //tax categories
            await _checkoutAttributeViewModelService.PrepareTaxCategories(model, checkoutAttribute, true);
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id, [FromServices] ICustomerActivityService customerActivityService)
        {
            var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(id);
            await _checkoutAttributeService.DeleteCheckoutAttribute(checkoutAttribute);

            //activity log
            await customerActivityService.InsertActivity("DeleteCheckoutAttribute", checkoutAttribute.Id, _translationService.GetResource("ActivityLog.DeleteCheckoutAttribute"), checkoutAttribute.Name);

            Success(_translationService.GetResource("Admin.Orders.CheckoutAttributes.Deleted"));
            return RedirectToAction("List");
        }

        #endregion

        #region Checkout attribute values

        //list
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> ValueList(string checkoutAttributeId, DataSourceRequest command)
        {
            var checkoutAttribute = await _checkoutAttributeViewModelService.PrepareCheckoutAttributeValuesModel(checkoutAttributeId);
            var gridModel = new DataSourceResult
            {
                Data = checkoutAttribute.ToList(),
                Total = checkoutAttribute.Count()
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueCreatePopup(string checkoutAttributeId)
        {
            var model = await _checkoutAttributeViewModelService.PrepareCheckoutAttributeValueModel(checkoutAttributeId);
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueCreatePopup(CheckoutAttributeValueModel model)
        {
            var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(model.CheckoutAttributeId);
            if (checkoutAttribute == null)
                //No checkout attribute found with the specified id
                return RedirectToAction("List");

            model.PrimaryStoreCurrencyCode = (await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)).CurrencyCode;
            model.BaseWeightIn = (await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)).Name;

            if (checkoutAttribute.AttributeControlTypeId == AttributeControlType.ColorSquares)
            {
                //ensure valid color is chosen/entered
                if (String.IsNullOrEmpty(model.ColorSquaresRgb))
                    ModelState.AddModelError("", "Color is required");
            }

            if (ModelState.IsValid)
            {
                var cav = await _checkoutAttributeViewModelService.InsertCheckoutAttributeValueModel(checkoutAttribute, model);
                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueEditPopup(string id, string checkoutAttributeId)
        {
            var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
            var cav = checkoutAttribute.CheckoutAttributeValues.Where(x => x.Id == id).FirstOrDefault();
            if (cav == null)
                //No checkout attribute value found with the specified id
                return RedirectToAction("List");

            var model = await _checkoutAttributeViewModelService.PrepareCheckoutAttributeValueModel(checkoutAttribute, cav);

            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueEditPopup(CheckoutAttributeValueModel model)
        {
            var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(model.CheckoutAttributeId);

            var cav = checkoutAttribute.CheckoutAttributeValues.Where(x => x.Id == model.Id).FirstOrDefault();
            if (cav == null)
                //No checkout attribute value found with the specified id
                return RedirectToAction("List");

            model.PrimaryStoreCurrencyCode = (await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)).CurrencyCode;
            model.BaseWeightIn = (await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)).Name;

            if (checkoutAttribute.AttributeControlTypeId == AttributeControlType.ColorSquares)
            {
                //ensure valid color is chosen/entered
                if (String.IsNullOrEmpty(model.ColorSquaresRgb))
                    ModelState.AddModelError("", "Color is required");
            }

            if (ModelState.IsValid)
            {
                await _checkoutAttributeViewModelService.UpdateCheckoutAttributeValueModel(checkoutAttribute, cav, model);
                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueDelete(string id, string checkoutAttributeId)
        {
            var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
            var cav = checkoutAttribute.CheckoutAttributeValues.Where(x => x.Id == id).FirstOrDefault();
            if (cav == null)
                throw new ArgumentException("No checkout attribute value found with the specified id");

            if (ModelState.IsValid)
            {
                checkoutAttribute.CheckoutAttributeValues.Remove(cav);
                await _checkoutAttributeService.UpdateCheckoutAttribute(checkoutAttribute);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }
        #endregion
    }
}
