using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Pages
{
    public partial class PageListModel : BaseModel
    {
        public PageListModel()
        {
            AvailableStores = new List<SelectListItem>();
        }
        [GrandResourceDisplayName("Admin.Content.Pages.List.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Content.Pages.List.SearchStore")]
        public string SearchStoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
    }
}