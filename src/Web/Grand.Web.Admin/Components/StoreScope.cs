using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.Components;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

public class StoreScopeViewComponent : BaseAdminViewComponent
{

    #region Fields

    private readonly IStoreService _storeService;
    private readonly IAdminStoreService _adminStoreService;
    private readonly ITranslationService _translationService;

    #endregion

    #region Constructors

    public StoreScopeViewComponent(
        IStoreService storeService,
        IAdminStoreService adminStoreService,
        ITranslationService translationService)
    {
        _adminStoreService = adminStoreService;
        _storeService = storeService;
        _translationService = translationService;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var allStores = await _storeService.GetAllStores();
        if (allStores.Count < 2)
            return Content("");

        var model = new StoreScopeModel();

        //global scope (all stores)
        model.Stores.Add(new StoreModel {
            Id = "",
            Name = _translationService.GetResource("Admin.Settings.StoreScope.AllStores")
        });

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