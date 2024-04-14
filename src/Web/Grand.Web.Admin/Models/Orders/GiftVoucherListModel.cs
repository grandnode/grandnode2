using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Models.Orders;

public class GiftVoucherListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.GiftVouchers.List.CouponCode")]

    public string CouponCode { get; set; }

    [GrandResourceDisplayName("Admin.GiftVouchers.List.RecipientName")]

    public string RecipientName { get; set; }

    [GrandResourceDisplayName("Admin.GiftVouchers.List.Activated")]
    public int ActivatedId { get; set; }

    [GrandResourceDisplayName("Admin.GiftVouchers.List.Activated")]
    public IList<SelectListItem> ActivatedList { get; set; } = new List<SelectListItem>();
}