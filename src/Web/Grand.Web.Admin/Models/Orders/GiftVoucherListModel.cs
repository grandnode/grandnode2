using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class GiftVoucherListModel : BaseModel
    {
        public GiftVoucherListModel()
        {
            ActivatedList = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.GiftVouchers.List.CouponCode")]
        
        public string CouponCode { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.List.RecipientName")]
        
        public string RecipientName { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.List.Activated")]
        public int ActivatedId { get; set; }
        [GrandResourceDisplayName("Admin.GiftVouchers.List.Activated")]
        public IList<SelectListItem> ActivatedList { get; set; }
    }
}