using Grand.Api.Models;

namespace Grand.Api.DTOs.Shipping;

public class ShippingMethodDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int DisplayOrder { get; set; }
}