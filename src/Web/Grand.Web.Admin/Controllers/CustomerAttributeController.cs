using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Customers;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Settings)]
    public partial class CustomerAttributeController : BaseAdminController
    {
        #region Fields
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly ICustomerAttributeViewModelService _customerAttributeViewModelService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        #endregion

        #region Constructors

        public CustomerAttributeController(ICustomerAttributeService customerAttributeService,
            ICustomerAttributeViewModelService customerAttributeViewModelService,
            ILanguageService languageService,
            ITranslationService translationService)
        {
            _customerAttributeService = customerAttributeService;
            _customerAttributeViewModelService = customerAttributeViewModelService;
            _languageService = languageService;
            _translationService = translationService;
        }

        #endregion

        #region Customer attributes

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var customerAttributes = await _customerAttributeViewModelService.PrepareCustomerAttributes();
            var gridModel = new DataSourceResult
            {
                Data = customerAttributes.ToList(),
                Total = customerAttributes.Count()
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = _customerAttributeViewModelService.PrepareCustomerAttributeModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create(CustomerAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var customerAttribute = await _customerAttributeViewModelService.InsertCustomerAttributeModel(model);
                Success(_translationService.GetResource("Admin.Customers.CustomerAttributes.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = customerAttribute.Id }) : RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(id);
            if (customerAttribute == null)
                //No customer attribute found with the specified id
                return RedirectToAction("List");

            var model = _customerAttributeViewModelService.PrepareCustomerAttributeModel(customerAttribute);
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = customerAttribute.GetTranslation(x => x.Name, languageId, false);
            });
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(CustomerAttributeModel model, bool continueEditing)
        {
            var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(model.Id);
            if (customerAttribute == null)
                //No customer attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                customerAttribute = await _customerAttributeViewModelService.UpdateCustomerAttributeModel(model, customerAttribute);

                Success(_translationService.GetResource("Admin.Customers.CustomerAttributes.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = customerAttribute.Id });
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
            await _customerAttributeViewModelService.DeleteCustomerAttribute(id);

            Success(_translationService.GetResource("Admin.Customers.CustomerAttributes.Deleted"));
            return RedirectToAction("List");
        }

        #endregion

        #region Customer attribute values

        //list
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> ValueList(string customerAttributeId, DataSourceRequest command)
        {
            var values = await _customerAttributeViewModelService.PrepareCustomerAttributeValues(customerAttributeId);
            var gridModel = new DataSourceResult
            {
                Data = values.ToList(),
                Total = values.Count()
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueCreatePopup(string customerAttributeId)
        {
            var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(customerAttributeId);
            if (customerAttribute == null)
                //No customer attribute found with the specified id
                return RedirectToAction("List");

            var model = _customerAttributeViewModelService.PrepareCustomerAttributeValueModel(customerAttributeId);
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueCreatePopup(CustomerAttributeValueModel model)
        {
            var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(model.CustomerAttributeId);
            if (customerAttribute == null)
                //No customer attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _customerAttributeViewModelService.InsertCustomerAttributeValueModel(model);
                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> ValueEditPopup(string id, string customerAttributeId)
        {
            var av = await _customerAttributeService.GetCustomerAttributeById(customerAttributeId);
            var cav = av.CustomerAttributeValues.FirstOrDefault(x => x.Id == id);
            if (cav == null)
                //No customer attribute value found with the specified id
                return RedirectToAction("List");

            var model = _customerAttributeViewModelService.PrepareCustomerAttributeValueModel(cav);
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueEditPopup(CustomerAttributeValueModel model)
        {
            var av = await _customerAttributeService.GetCustomerAttributeById(model.CustomerAttributeId);
            var cav = av.CustomerAttributeValues.FirstOrDefault(x => x.Id == model.Id);
            if (cav == null)
                //No customer attribute value found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _customerAttributeViewModelService.UpdateCustomerAttributeValueModel(model, cav);

                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ValueDelete(CustomerAttributeValueModel model)
        {
            await _customerAttributeViewModelService.DeleteCustomerAttributeValue(model);

            return new JsonResult("");
        }
        #endregion
    }
}
