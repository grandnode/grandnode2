using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Orders;

public class MerchandiseReturnActionDto : BaseApiEntityModel
{
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
    public bool LimitedToStores { get; set; }
    public IList<string> Stores { get; set; } = new List<string>();
}
