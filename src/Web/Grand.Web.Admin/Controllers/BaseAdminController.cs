using Grand.Business.Common.Interfaces.Stores;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [AuthorizeAdmin]
    [AutoValidateAntiforgeryToken]
    [Area(Constants.AreaAdmin)]
    [AuthorizeVendor]
    public abstract partial class BaseAdminController : BaseController
    {

        /// <summary>
        /// Save selected TAB index
        /// </summary>
        /// <param name="index">Idnex to save; null to automatically detect it</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected async Task SaveSelectedTabIndex(int? index = null, bool persistForTheNextRequest = true)
        {
            if (!index.HasValue)
            {
                int tmp;
                var form = await HttpContext.Request.ReadFormAsync();
                var tabindex = form["selected-tab-index"];
                if (tabindex.Count > 0)
                {
                    if (int.TryParse(tabindex[0], out tmp))
                    {
                        index = tmp;
                    }
                }
                else
                    index = 1;
            }
            if (index.HasValue)
            {
                string dataKey = "Grand.selected-tab-index";
                if (persistForTheNextRequest)
                {
                    TempData[dataKey] = index;
                }
                else
                {
                    ViewData[dataKey] = index;
                }
            }
        }

        /// <summary>
        /// Get active store scope (for multi-store configuration mode)
        /// </summary>
        /// <param name="storeService">Store service</param>
        /// <param name="workContext">Work context</param>
        /// <returns>Store ID; 0 if we are in a shared mode</returns>
        protected virtual async Task<string> GetActiveStore(IStoreService storeService, IWorkContext workContext)
        {
            var stores = await storeService.GetAllStores();
            if (stores.Count < 2)
                return stores.FirstOrDefault().Id;

            var storeId = workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration);
            var store = await storeService.GetStoreById(storeId);

            return store != null ? store.Id : "";
        }
        /// <summary>
        /// Creates a <see cref="T:System.Web.Mvc.JsonResult"/> object that serializes the specified object to JavaScript Object Notation (JSON) format using the content type, content encoding, and the JSON request behavior.
        /// </summary>
        /// 
        /// <returns>
        /// The result object that serializes the specified object to JSON format.
        /// </returns>
        /// <param name="data">The JavaScript object graph to serialize.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <param name="contentEncoding">The content encoding.</param>
        /// <param name="behavior">The JSON request behavior</param>
        public override JsonResult Json(object data)
        {
            var serializerSettings = new JsonSerializerSettings {
                DateFormatHandling = DateFormatHandling.IsoDateFormat 
            };
            return base.Json(data, serializerSettings);
        }
    }
}