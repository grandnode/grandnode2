using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Store.Models.Pages;

public class PageListModel : BaseModel
{
    [GrandResourceDisplayName("Store.Content.Pages.Fields.Name")]
    public string Name { get; set; }
}
