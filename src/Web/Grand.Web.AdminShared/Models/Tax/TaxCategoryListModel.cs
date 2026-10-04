using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Models.Tax;

public class TaxCategoryListModel
{
    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
}
