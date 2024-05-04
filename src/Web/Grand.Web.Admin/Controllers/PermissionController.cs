using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Models.Permissions;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Acl)]
public class PermissionController : BaseAdminController
{
    #region Constructors

    public PermissionController(IWorkContext workContext,
        IPermissionService permissionService,
        IGroupService groupService,
        ITranslationService translationService)
    {
        _workContext = workContext;
        _permissionService = permissionService;
        _groupService = groupService;
        _translationService = translationService;
    }

    #endregion

    #region Fields

    private readonly IWorkContext _workContext;
    private readonly IPermissionService _permissionService;
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;

    #endregion

    #region Methods

    public async Task<IActionResult> Index()
    {
        var model = new PermissionMappingModel();

        var permissionRecords = await _permissionService.GetAllPermissions();
        var customerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);
        foreach (var pr in permissionRecords.OrderBy(x => x.Category))
            model.AvailablePermissions.Add(new PermissionRecordModel {
                Name = pr.GetTranslationPermissionName(_translationService, _workContext),
                SystemName = pr.SystemName,
                Area = pr.Area,
                Category = pr.Category,
                Actions = pr.Actions.Any()
            });

        foreach (var cr in customerGroups)
            model.AvailableCustomerGroups.Add(new CustomerGroupModel { Id = cr.Id, Name = cr.Name });

        foreach (var pr in permissionRecords)
        foreach (var cr in customerGroups)
        {
            var allowed = pr.CustomerGroups.Count(x => x == cr.Id) > 0;
            if (!model.Allowed.ContainsKey(pr.SystemName))
                model.Allowed[pr.SystemName] = new Dictionary<string, bool>();
            model.Allowed[pr.SystemName][cr.Id] = allowed;
        }

        return View(model);
    }

    public IActionResult Create()
    {
        return View(new PermissionCreateModel { Area = "Area admin" });
    }

    [HttpPost]
    public async Task<IActionResult> Create(PermissionCreateModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var permission = model.ToEntity();
        await _permissionService.InsertPermission(permission);
        return Content("");
    }

    public async Task<IActionResult> Update(string systemName)
    {
        if (string.IsNullOrEmpty(systemName)) return Content("SystemName is null");

        var permission = await _permissionService.GetPermissionBySystemName(systemName);
        if (permission == null) return Content("Permission not found");
        var model = permission.ToModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Update(PermissionUpdateModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var permission = await _permissionService.GetPermissionById(model.Id);
        permission = model.ToEntity(permission);
        await _permissionService.UpdatePermission(permission);
        return Content("");
    }

    [HttpPost]
    [ActionName("Index")]
    public async Task<IActionResult> PermissionsSave(IDictionary<string, string[]> model)
    {
        var permissionRecords = await _permissionService.GetAllPermissions();
        var customerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);

        foreach (var cr in customerGroups)
        {
            model.TryGetValue($"allow_{cr.Id}", out var permissionIds);
            var permissionRecordSystemNamesToRestrict =
                permissionIds != null ? permissionIds.ToList() : new List<string>();
            foreach (var pr in permissionRecords)
            {
                var allow = permissionRecordSystemNamesToRestrict.Contains(pr.SystemName);
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

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> PermissionsAction(string systemName, string customeGroupId)
    {
        var model = new PermissionActionModel {
            SystemName = systemName,
            CustomerGroupId = customeGroupId
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

        model.DeniedActions = (await _permissionService.GetPermissionActions(systemName, customeGroupId))
            .Select(x => x.Action).ToList();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> PermissionsAction(PermissionActionSaveModel model)
    {
        //remove denied actions
        var deniedActions = await _permissionService.GetPermissionActions(model.SystemName, model.CustomerGroupId);
        foreach (var action in deniedActions) await _permissionService.DeletePermissionAction(action);

        //insert denied actions
        var permissionRecord = await _permissionService.GetPermissionBySystemName(model.SystemName);
        var insertActions = permissionRecord.Actions.Except(model.SelectedActions ?? new List<string>());

        foreach (var item in insertActions)
            await _permissionService.InsertPermissionAction(new PermissionAction {
                Action = item,
                CustomerGroupId = model.CustomerGroupId,
                SystemName = model.SystemName
            });

        ViewBag.ClosePage = true;
        return await PermissionsAction(model.SystemName, model.CustomerGroupId);
    }

    #endregion
}