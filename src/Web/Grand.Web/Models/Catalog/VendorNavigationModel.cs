﻿using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog
{
    public partial class VendorNavigationModel : BaseModel
    {
        public VendorNavigationModel()
        {
            Vendors = new List<VendorBriefInfoModel>();
        }

        public IList<VendorBriefInfoModel> Vendors { get; set; }

        public int TotalVendors { get; set; }
    }

    public partial class VendorBriefInfoModel : BaseEntityModel
    {
        public string Name { get; set; }

        public string SeName { get; set; }
    }
}