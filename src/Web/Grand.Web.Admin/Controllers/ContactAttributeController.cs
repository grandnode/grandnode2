using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.ContactAttributes)]
    public partial class ContactAttributeController : BaseAdminController
    {
        #region Fields
        private readonly IContactAttributeViewModelService _contactAttributeViewModelService;
        private readonly IContactAttributeService _contactAttributeService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IStoreService _storeService;
        private readonly IGroupService _groupService;

        #endregion

        #region Constructors

        public ContactAttributeController(IContactAttributeViewModelService contactAttributeViewModelService,
            IContactAttributeService contactAttributeService,
            ILanguageService languageService,
            ITranslationService translationService,
            IStoreService storeService,
            IGroupService groupService)
        {
            _contactAttributeViewModelService = contactAttributeViewModelService;
            _contactAttributeService = contactAttributeService;
            _languageService = languageService;
            _translationService = translationService;
            _storeService = storeService;
            _groupService = groupService;
        }

        #endregion

        #region Contact attributes

        //list
        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public IActionResult List() => View();

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var contactAttributes = await _contactAttributeViewModelService.PrepareContactAttributeListModel();
            var gridModel = new DataSourceResult
            {
                Data = contactAttributes.ToList(),
                Total = contactAttributes.Count()
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new ContactAttributeModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            //condition
            await _contactAttributeViewModelService.PrepareConditionAttributes(model, null);

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create(ContactAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var contactAttribute = await _contactAttributeViewModelService.InsertContactAttributeModel(model);
                Success(_translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = contactAttribute.Id }) : RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var contactAttribute = await _contactAttributeService.GetContactAttributeById(id);
            if (contactAttribute == null)
                //No contact attribute found with the specified id
                return RedirectToAction("List");

            var model = contactAttribute.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = contactAttribute.GetTranslation(x => x.Name, languageId, false);
                locale.TextPrompt = contactAttribute.GetTranslation(x => x.TextPrompt, languageId, false);
            });
            //condition
            await _contactAttributeViewModelService.PrepareConditionAttributes(model, contactAttribute);

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(ContactAttributeModel model, bool continueEditing)
        {
            var contactAttribute = await _contactAttributeService.GetContactAttributeById(model.Id);
            if (contactAttribute == null)
                //No contact attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                contactAttribute = await _contactAttributeViewModelService.UpdateContactAttributeModel(contactAttribute, model);
                Success(_translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = contactAttribute.Id });
                }
                return RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            await _contactAttributeViewModelService.PrepareConditionAttributes(model, contactAttribute);
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id, [FromServices] ICustomerActivityService customerActivityService)
        {
            if (ModelState.IsValid)
            {
                var contactAttribute = await _contactAttributeService.GetContactAttributeById(id);
                await _contactAttributeService.DeleteContactAttribute(contactAttribute);

                //activity log
                await customerActivityService.InsertActivity("DeleteContactAttribute", contactAttribute.Id, _translationService.GetResource("ActivityLog.DeleteContactAttribute"), contactAttribute.Name);

                Success(_translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = id });
        }

        #endregion

        #region Contact attribute values

        //list
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> ValueList(string contactAttributeId, DataSourceRequest command)
        {
            var contactAttribute = await _contactAttributeService.GetContactAttributeById(contactAttributeId);
            var values = contactAttribute.ContactAttributeValues;
            var gridModel = new DataSourceResult
            {
                Data = values.Select(x => new ContactAttributeValueModel
                {
                    Id = x.Id,
                    ContactAttributeId = x.ContactAttributeId,
                    Name = contactAttribute.AttributeControlType != AttributeControlType.ColorSquares ? x.Name : string.Format("{0} - {1}", x.Name, x.ColorSquaresRgb),
                    ColorSquaresRgb = x.ColorSquaresRgb,
                    IsPreSelected = x.IsPreSelected,
                    DisplayOrder = x.DisplayOrder,
                }),
                Total = values.Count()
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueCreatePopup(string contactAttributeId)
        {
            var contactAttribute = await _contactAttributeService.GetContactAttributeById(contactAttributeId);
            var model = _contactAttributeViewModelService.PrepareContactAttributeValueModel(contactAttribute);

            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueCreatePopup(ContactAttributeValueModel model)
        {
            var contactAttribute = await _contactAttributeService.GetContactAttributeById(model.ContactAttributeId);
            if (contactAttribute == null)
                //No contact attribute found with the specified id
                return RedirectToAction("List");

            if (contactAttribute.AttributeControlType == AttributeControlType.ColorSquares)
            {
                //ensure valid color is chosen/entered
                if (String.IsNullOrEmpty(model.ColorSquaresRgb))
                    ModelState.AddModelError("", "Color is required");
            }

            if (ModelState.IsValid)
            {
                await _contactAttributeViewModelService.InsertContactAttributeValueModel(contactAttribute, model);

                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueEditPopup(string id, string contactAttributeId)
        {
            var contactAttribute = await _contactAttributeService.GetContactAttributeById(contactAttributeId);
            var cav = contactAttribute.ContactAttributeValues.Where(x => x.Id == id).FirstOrDefault();
            if (cav == null)
                //No contact attribute value found with the specified id
                return RedirectToAction("List");

            var model = _contactAttributeViewModelService.PrepareContactAttributeValueModel(contactAttribute, cav);
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueEditPopup(ContactAttributeValueModel model)
        {
            var contactAttribute = await _contactAttributeService.GetContactAttributeById(model.ContactAttributeId);

            var cav = contactAttribute.ContactAttributeValues.Where(x => x.Id == model.Id).FirstOrDefault();
            if (cav == null)
                //No contact attribute value found with the specified id
                return RedirectToAction("List");

            if (contactAttribute.AttributeControlType == AttributeControlType.ColorSquares)
            {
                //ensure valid color is chosen/entered
                if (String.IsNullOrEmpty(model.ColorSquaresRgb))
                    ModelState.AddModelError("", "Color is required");
            }

            if (ModelState.IsValid)
            {
                await _contactAttributeViewModelService.UpdateContactAttributeValueModel(contactAttribute, cav, model);
                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueDelete(string id, string contactAttributeId)
        {
            var contactAttribute = await _contactAttributeService.GetContactAttributeById(contactAttributeId);
            var cav = contactAttribute.ContactAttributeValues.Where(x => x.Id == id).FirstOrDefault();
            if (cav == null)
                throw new ArgumentException("No contact attribute value found with the specified id");
            if (ModelState.IsValid)
            {
                contactAttribute.ContactAttributeValues.Remove(cav);
                await _contactAttributeService.UpdateContactAttribute(contactAttribute);

                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }
        #endregion
    }
}
