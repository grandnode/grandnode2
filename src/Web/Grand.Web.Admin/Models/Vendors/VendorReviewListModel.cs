using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class VendorReviewListModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.VendorReviews.List.CreatedOnFrom")]
        [UIHint("DateNullable")]
        public DateTime? CreatedOnFrom { get; set; }

        [GrandResourceDisplayName("Admin.VendorReviews.List.CreatedOnTo")]
        [UIHint("DateNullable")]
        public DateTime? CreatedOnTo { get; set; }

        [GrandResourceDisplayName("Admin.VendorReviews.List.SearchText")]
        public string SearchText { get; set; }

        [GrandResourceDisplayName("Admin.VendorReviews.List.SearchVendor")]
        public string SearchVendorId { get; set; }

    }
}