using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Vendor.Models.Common;

namespace Grand.Web.Vendor.Models.Vendor
{
    public class VendorModel : BaseEntityModel, ILocalizedModel<VendorLocalizedModel>
    {
        public VendorModel()
        {
            Locales = new List<VendorLocalizedModel>();
            Address = new AddressModel();
        }

        [GrandResourceDisplayName("Vendor.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.Email")]

        public string Email { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.Description")]

        public string Description { get; set; }
        
        [GrandResourceDisplayName("Vendor.Fields.MetaKeywords")]

        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.MetaDescription")]

        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.MetaTitle")]

        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.SeName")]

        public string SeName { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.PageSize")]
        public int PageSize { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.AllowCustomersToSelectPageSize")]
        public bool AllowCustomersToSelectPageSize { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.PageSizeOptions")]
        public string PageSizeOptions { get; set; }

        public IList<VendorLocalizedModel> Locales { get; set; }

        public AddressModel Address { get; set; }
        
    }

    public class VendorLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.Description")]

        public string Description { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.MetaKeywords")]

        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.MetaDescription")]

        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.MetaTitle")]

        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Vendor.Fields.SeName")]

        public string SeName { get; set; }
    }
}