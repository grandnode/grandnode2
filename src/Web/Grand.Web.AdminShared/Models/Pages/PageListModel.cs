using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Pages;

public class PageListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Content.Pages.List.Name")]
    public string Name { get; set; }
}