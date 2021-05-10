using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.AddressAttributes)]
    public partial class AddressAttributeController : BaseAdminController
    {
        #region Fields

        private readonly IAddressAttributeService _addressAttributeService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IAddressAttributeViewModelService _addressAttributeViewModelService;
        #endregion

        #region Constructors

        public AddressAttributeController(IAddressAttributeService addressAttributeService,
            ILanguageService languageService,
            ITranslationService translationService,
            IAddressAttributeViewModelService addressAttributeViewModelService)
        {
            _addressAttributeService = addressAttributeService;
            _languageService = languageService;
            _translationService = translationService;
            _addressAttributeViewModelService = addressAttributeViewModelService;
        }

        #endregion

        #region Address attributes

        public IActionResult Index() => RedirectToAction("List");


        public IActionResult List() => View();

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var model = await _addressAttributeViewModelService.PrepareAddressAttributes();
            var gridModel = new DataSourceResult
            {
                Data = model.addressAttributes,
                Total = model.totalCount
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = _addressAttributeViewModelService.PrepareAddressAttributeModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(AddressAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var addressAttribute = await _addressAttributeViewModelService.InsertAddressAttributeModel(model);
                Success(_translationService.GetResource("Admin.Address.AddressAttributes.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = addressAttribute.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        //edit
        public async Task<IActionResult> Edit(string id)
        {
            var addressAttribute = await _addressAttributeService.GetAddressAttributeById(id);
            if (addressAttribute == null)
                //No address attribute found with the specified id
                return RedirectToAction("List");

            var model = _addressAttributeViewModelService.PrepareAddressAttributeModel(addressAttribute);
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = addressAttribute.GetTranslation(x => x.Name, languageId, false);
            });
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(AddressAttributeModel model, bool continueEditing)
        {
            var addressAttribute = await _addressAttributeService.GetAddressAttributeById(model.Id);
            if (addressAttribute == null)
                //No address attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                addressAttribute = await _addressAttributeViewModelService.UpdateAddressAttributeModel(model, addressAttribute);
                Success(_translationService.GetResource("Admin.Address.AddressAttributes.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = addressAttribute.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            var addressAttribute = await _addressAttributeService.GetAddressAttributeById(id);
            if (addressAttribute == null)
                //No address attribute found with the specified id
                return RedirectToAction("List");

            await _addressAttributeService.DeleteAddressAttribute(addressAttribute);

            Success(_translationService.GetResource("Admin.Address.AddressAttributes.Deleted"));
            return RedirectToAction("List");
        }

        #endregion

        #region Address attribute values

        //list
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> ValueList(string addressAttributeId, DataSourceRequest command)
        {
            var model = await _addressAttributeViewModelService.PrepareAddressAttributeValues(addressAttributeId);
            var gridModel = new DataSourceResult
            {
                Data = model.addressAttributeValues,
                Total = model.totalCount
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> ValueCreatePopup(string addressAttributeId)
        {
            var addressAttribute = await _addressAttributeService.GetAddressAttributeById(addressAttributeId);
            if (addressAttribute == null)
                //No address attribute found with the specified id
                return RedirectToAction("List");

            var model = _addressAttributeViewModelService.PrepareAddressAttributeValueModel(addressAttributeId);
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> ValueCreatePopup(AddressAttributeValueModel model)
        {
            var addressAttribute = await _addressAttributeService.GetAddressAttributeById(model.AddressAttributeId);
            if (addressAttribute == null)
                //No address attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _addressAttributeViewModelService.InsertAddressAttributeValueModel(model);
                ViewBag.RefreshPage = true;
                return View(model);
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> ValueEditPopup(string id, string addressAttributeId)
        {
            var av = await _addressAttributeService.GetAddressAttributeById(addressAttributeId);
            if (av == null)
                //No address attribute found with the specified id
                return RedirectToAction("List");

            var cav = av.AddressAttributeValues.FirstOrDefault(x => x.Id == id);
            if (cav == null)
                //No address attribute value found with the specified id
                return RedirectToAction("List");

            var model = _addressAttributeViewModelService.PrepareAddressAttributeValueModel(cav);

            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueEditPopup(AddressAttributeValueModel model)
        {
            var av = await _addressAttributeService.GetAddressAttributeById(model.AddressAttributeId);
            var cav = av.AddressAttributeValues.FirstOrDefault(x => x.Id == model.Id);
            if (cav == null)
                //No address attribute value found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _addressAttributeViewModelService.UpdateAddressAttributeValueModel(model, cav);
                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueDelete(AddressAttributeValueModel model)
        {
            var av = await _addressAttributeService.GetAddressAttributeById(model.AddressAttributeId);
            var cav = av.AddressAttributeValues.FirstOrDefault(x => x.Id == model.Id);
            if (cav == null)
                throw new ArgumentException("No address attribute value found with the specified id");
            await _addressAttributeService.DeleteAddressAttributeValue(cav);

            return new JsonResult("");
        }
        #endregion
    }
}
