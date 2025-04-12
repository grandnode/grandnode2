using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Menu;

public class MenuListModel : BaseEntityModel
{
    public string SystemName { get; set; }
    public int DisplayOrder { get; set; }
}