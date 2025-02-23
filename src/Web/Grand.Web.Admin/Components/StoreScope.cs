using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Infrastructure;
using Grand.Web.Admin.Models.Settings;
using Grand.Web.Common.Components;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

public class StoreScopeViewComponent : BaseAdminViewComponent
{

    #region Fields

    private readonly IStoreService _storeService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IGroupService _groupService;
    private readonly IAdminStoreService _adminStoreService;

    #endregion

    #region Constructors

    public StoreScopeViewComponent(
        IStoreService storeService,
        IAdminStoreService adminStoreService,
        IGroupService groupService,
        IContextAccessor contextAccessor)
    {
        _adminStoreService = adminStoreService;
        _storeService = storeService;
        _contextAccessor = contextAccessor;
        _groupService = groupService;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var allStores = await _storeService.GetAllStores();
        if (allStores.Count < 2)
            return Content("");

        if (await _groupService.IsStaff(_contextAccessor.WorkContext.CurrentCustomer))
            allStores = allStores.Where(x => x.Id == _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId).ToList();

        var model = new StoreScopeModel();
        foreach (var s in allStores)
            model.Stores.Add(new StoreModel {
                Id = s.Id,
                Name = s.Shortcut
            });

        model.StoreId = await _adminStoreService.GetActiveStore();
        return View(model);
    }
    #endregion
}