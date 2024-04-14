using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Models.Discounts;

public class DiscountListModel : BaseModel
{
    [GrandResourceDisplayName("admin.marketing.Discounts.List.SearchDiscountCouponCode")]

    public string SearchDiscountCouponCode { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.List.SearchDiscountName")]

    public string SearchDiscountName { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.List.SearchDiscountType")]
    public int SearchDiscountTypeId { get; set; }

    public IList<SelectListItem> AvailableDiscountTypes { get; set; } = new List<SelectListItem>();
}