using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Settings
{
    public partial class StoreScopeModel : BaseModel
    {
        public StoreScopeModel()
        {
            Stores = new List<StoreModel>();
        }

        public string StoreId { get; set; }
        public IList<StoreModel> Stores { get; set; }
    }
}