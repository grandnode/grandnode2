using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class MessageTemplateListModel : BaseModel
    {
        public MessageTemplateListModel()
        {
            AvailableStores = new List<SelectListItem>();
        }
        [GrandResourceDisplayName("Admin.Content.MessageTemplates.List.Name")]
        public string Name { get; set; }
        [GrandResourceDisplayName("Admin.Content.MessageTemplates.List.SearchStore")]
        public string SearchStoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
    }
}