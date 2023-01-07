using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog
{
    public class VendorNavigationModel : BaseModel
    {
        public VendorNavigationModel()
        {
            Vendors = new List<VendorBriefInfoModel>();
        }

        public IList<VendorBriefInfoModel> Vendors { get; set; }

        public int TotalVendors { get; set; }
    }

    public class VendorBriefInfoModel : BaseEntityModel
    {
        public string Name { get; set; }

        public string SeName { get; set; }
    }
}