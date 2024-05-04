using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog;

public class VendorNavigationModel : BaseModel
{
    public IList<VendorBriefInfoModel> Vendors { get; set; } = new List<VendorBriefInfoModel>();

    public int TotalVendors { get; set; }
}

public class VendorBriefInfoModel : BaseEntityModel
{
    public string Name { get; set; }

    public string SeName { get; set; }
}