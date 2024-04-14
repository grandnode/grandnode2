using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Settings;

public class StoreScopeModel : BaseModel
{
    public string StoreId { get; set; }
    public IList<StoreModel> Stores { get; set; } = new List<StoreModel>();
}