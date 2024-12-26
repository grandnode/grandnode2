﻿using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.Admin.Models.Settings;
using Grand.Web.Common.Components;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

public class StoreScopeViewComponent : BaseAdminViewComponent
{
    #region Constructors

    public StoreScopeViewComponent(IStoreService storeService, IWorkContextAccessor workContextAccessor, IGroupService groupService)
    {
        _storeService = storeService;
        _workContext = workContextAccessor.WorkContext;
        _groupService = groupService;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var allStores = await _storeService.GetAllStores();
        if (allStores.Count < 2)
            return Content("");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            allStores = allStores.Where(x => x.Id == _workContext.CurrentCustomer.StaffStoreId).ToList();

        var model = new StoreScopeModel();
        foreach (var s in allStores)
            model.Stores.Add(new StoreModel {
                Id = s.Id,
                Name = s.Shortcut
            });
        model.StoreId = await GetActiveStore(allStores);
        return View(model);
    }

    #endregion

    #region Methods

    private async Task<string> GetActiveStore(ICollection<Store> stores)
    {
        //ensure that we have 2 (or more) stores
        if (stores.Count < 2)
            return stores.FirstOrDefault()!.Id;

        var storeId =
            _workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames
                .AdminAreaStoreScopeConfiguration);
        var store = await _storeService.GetStoreById(storeId);

        return store != null ? store.Id : "";
    }

    #endregion

    #region Fields

    private readonly IStoreService _storeService;
    private readonly IWorkContext _workContext;
    private readonly IGroupService _groupService;

    #endregion
}