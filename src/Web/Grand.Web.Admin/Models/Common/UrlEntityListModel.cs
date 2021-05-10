using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Common
{
    public partial class UrlEntityListModel : BaseModel
    {
        public UrlEntityListModel()
        {
            AvailableActiveOptions = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("admin.configuration.senames.Name")]
        public string SeName { get; set; }

        [GrandResourceDisplayName("admin.configuration.senames.Active")]
        public int SearchActiveId { get; set; }
        public IList<SelectListItem> AvailableActiveOptions { get; set; }
    }
}