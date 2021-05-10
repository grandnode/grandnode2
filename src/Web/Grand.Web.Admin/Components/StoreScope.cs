using Grand.Business.Common.Interfaces.Stores;
using Grand.Infrastructure;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Common.Components;
using Grand.Web.Admin.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Components
{
    public class StoreScopeViewComponent : BaseAdminViewComponent
    {
        #region Fields

        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Constructors

        public StoreScopeViewComponent(IStoreService storeService, IWorkContext workContext)
        {
            _storeService = storeService;
            _workContext = workContext;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var allStores = await _storeService.GetAllStores();
            if (allStores.Count < 2)
                return Content("");

            var model = new StoreScopeModel();
            foreach (var s in allStores)
            {
                model.Stores.Add(new Common.Models.StoreModel
                {
                    Id = s.Id,
                    Name = s.Shortcut
                });
            }
            model.StoreId = await GetActiveStore(_storeService, _workContext);
            return View(model);
        }

        #endregion

        #region Methods

        private async Task<string> GetActiveStore(IStoreService storeService, IWorkContext workContext)
        {
            //ensure that we have 2 (or more) stores
            if ((await storeService.GetAllStores()).Count < 2)
                return string.Empty;

            var storeId = workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration);
            var store = await storeService.GetStoreById(storeId);

            return store != null ? store.Id : "";
        }

        #endregion
    }
}