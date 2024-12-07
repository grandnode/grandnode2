using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Shipping;

public class DeliveryDateDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public int DisplayOrder { get; set; }
    public string ColorSquaresRgb { get; set; }
}