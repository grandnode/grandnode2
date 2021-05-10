using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.System.Commands.Models.Security;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Admin.Models.Permissions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Acl)]
    public partial class PermissionController : BaseAdminController
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService;
        private readonly IGroupService _groupService;
        private readonly ITranslationService _translationService;
        private readonly IMediator _mediator;

        #endregion

        #region Constructors

        public PermissionController(
            ILogger logger,
            IWorkContext workContext,
            IPermissionService permissionService,
            IGroupService groupService,
            ITranslationService translationService,
            IMediator mediator)
        {
            _logger = logger;
            _workContext = workContext;
            _permissionService = permissionService;
            _groupService = groupService;
            _translationService = translationService;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> Index()
        {
            var model = new PermissionMappingModel();

            var permissionRecords = await _permissionService.GetAllPermissions();
            var customerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);
            foreach (var pr in permissionRecords.OrderBy(x=>x.Category))
            {
                model.AvailablePermissions.Add(new PermissionRecordModel
                {
                    Name = pr.GetTranslationPermissionName(_translationService, _workContext),
                    SystemName = pr.SystemName,
                    Area = pr.Area,
                    Category = pr.Category,
                    Actions = pr.Actions.Any()
                });
            }
            foreach (var cr in customerGroups)
            {
                model.AvailableCustomerGroups.Add(new CustomerGroupModel() { Id = cr.Id, Name = cr.Name });
            }
            foreach (var pr in permissionRecords)
                foreach (var cr in customerGroups)
                {
                    bool allowed = pr.CustomerGroups.Count(x => x == cr.Id) > 0;
                    if (!model.Allowed.ContainsKey(pr.SystemName))
                        model.Allowed[pr.SystemName] = new Dictionary<string, bool>();
                    model.Allowed[pr.SystemName][cr.Id] = allowed;
                }

            return View(model);
        }

        [HttpPost, ActionName("Index"), ArgumentNameFilter(KeyName = "save-continue", Argument = "install")]
        public async Task<IActionResult> PermissionsSave(IFormCollection form, bool install)
        {
            if (!install)
            {
                var permissionRecords = await _permissionService.GetAllPermissions();
                var customerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);

                foreach (var cr in customerGroups)
                {
                    string formKey = "allow_" + cr.Id;
                    var permissionRecordSystemNamesToRestrict = form[formKey].ToString() != null ? form[formKey].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>();
                    foreach (var pr in permissionRecords)
                    {

                        bool allow = permissionRecordSystemNamesToRestrict.Contains(pr.SystemName);
                        if (allow)
                        {

                            if (pr.CustomerGroups.FirstOrDefault(x => x == cr.Id) == null)
                            {
                                pr.CustomerGroups.Add(cr.Id);
                                await _permissionService.UpdatePermission(pr);
                            }
                        }
                        else
                        {
                            if (pr.CustomerGroups.FirstOrDefault(x => x == cr.Id) != null)
                            {
                                pr.CustomerGroups.Remove(cr.Id);
                                await _permissionService.UpdatePermission(pr);
                            }
                        }
                    }
                }
                Success(_translationService.GetResource("Admin.Configuration.Permissions.Updated"));
            }
            else
            {
                IPermissionProvider provider = new PermissionProvider();
                await _mediator.Send(new InstallNewPermissionsCommand() { PermissionProvider = provider });

                Success(_translationService.GetResource("Admin.Configuration.Permissions.Installed"));
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> PermissionsAction(string systemName, string customeGroupId)
        {
            var model = new PermissionActionModel()
            {
                SystemName = systemName,
                CustomerGroupId = customeGroupId,
            };

            var customerGroup = await _groupService.GetCustomerGroupById(customeGroupId);
            if (customerGroup != null)
            {
                model.CustomerGroupName = customerGroup.Name;
            }
            else
            {
                ViewBag.ClosePage = true;
                return await PermissionsAction(systemName, customeGroupId);
            }

            var permissionRecord = await _permissionService.GetPermissionBySystemName(systemName);
            if (permissionRecord != null)
            {
                model.AvailableActions = permissionRecord.Actions.ToList();
                model.PermissionName = permissionRecord.GetTranslationPermissionName(_translationService, _workContext);
            }
            else
            {
                ViewBag.ClosePage = true;
                return await PermissionsAction(systemName, customeGroupId);
            }

            model.DeniedActions = (await _permissionService.GetPermissionActions(systemName, customeGroupId)).Select(x => x.Action).ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PermissionsAction(IFormCollection form)
        {
            var systemname = form["SystemName"].ToString();
            var customergroupId = form["CustomerGroupId"].ToString();

            var selected = form["SelectedActions"].ToList();

            //remove denied actions
            var deniedActions = await _permissionService.GetPermissionActions(systemname, customergroupId);
            foreach (var action in deniedActions)
            {
                await _permissionService.DeletePermissionAction(action);
            }

            //insert denied actions
            var permissionRecord = await _permissionService.GetPermissionBySystemName(systemname);
            var insertActions = permissionRecord.Actions.Except(selected);

            foreach (var item in insertActions)
            {
                await _permissionService.InsertPermissionAction(new PermissionAction()
                {
                    Action = item,
                    CustomerGroupId = customergroupId,
                    SystemName = systemname
                });
            }
            ViewBag.ClosePage = true;
            return await PermissionsAction(systemname, customergroupId);
        }
        #endregion
    }
}
