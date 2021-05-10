using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Admin.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Actions)]
    public partial class CustomerActionTypeController : BaseAdminController
    {
        #region Fields
        private readonly ICustomerActionService _customerActionService;
        private readonly ITranslationService _translationService;
        #endregion Fields

        #region Constructors

        public CustomerActionTypeController(
            ITranslationService translationService,
            ICustomerActionService customerActionService
            )
        {
            _customerActionService = customerActionService;
            _translationService = translationService;
        }

        #endregion

        #region Action types

        public async Task<IActionResult> ListTypes()
        {
            var model = (await _customerActionService.GetCustomerActionType())
                .Select(x => x.ToModel())
                .ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveTypes(IFormCollection form)
        {
            string formKey = "checkbox_action_types";
            var checkedActionTypes = form[formKey].ToString() != null ? form[formKey].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x).ToList() : new List<string>();

            var activityTypes = await _customerActionService.GetCustomerActionType();
            foreach (var actionType in activityTypes)
            {
                actionType.Enabled = checkedActionTypes.Contains(actionType.Id);
                await _customerActionService.UpdateCustomerActionType(actionType);
            }
            Success(_translationService.GetResource("Admin.Customers.ActionType.Updated"));
            return RedirectToAction("ListTypes");
        }

        #endregion


    }
}
