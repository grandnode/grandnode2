using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Models.Orders;

public class RecurringPaymentListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.List.SearchCustomerEmail")]
    public string SearchCustomerEmail { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.List.SearchStatus")]
    public int SearchStatusId { get; set; } = -1;

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.List.SearchStore")]
    public string StoreId { get; set; }

    /// <summary>
    ///     False in the store panel, which sees its own store only
    /// </summary>
    public bool ShowStoreFilter { get; set; }

    public IList<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
}
