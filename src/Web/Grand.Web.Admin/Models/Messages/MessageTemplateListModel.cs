using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Models.Messages;

public class MessageTemplateListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Content.MessageTemplates.List.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Content.MessageTemplates.List.SearchStore")]
    public string SearchStoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
}