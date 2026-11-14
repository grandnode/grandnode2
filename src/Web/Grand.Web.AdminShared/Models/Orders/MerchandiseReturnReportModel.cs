using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Orders;

public class MerchandiseReturnReportModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Reports.MerchandiseReturns.Store")]
    public string StoreId { get; set; }

    [GrandResourceDisplayName("Admin.Reports.MerchandiseReturns.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Admin.Reports.MerchandiseReturns.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }

    //a MerchandiseReturnReportGrouping value
    [GrandResourceDisplayName("Admin.Reports.MerchandiseReturns.GroupBy")]
    public int GroupBy { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();

    public IList<SelectListItem> AvailableGroupings { get; set; } = new List<SelectListItem>();
}
