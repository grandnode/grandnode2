using Grand.Api.Models;

namespace Grand.Api.DTOs.Catalog;

public class ProductAttributeDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public string Description { get; set; }

    public IList<PredefinedProductAttributeValueDto> PredefinedProductAttributeValues { get; set; } =
        new List<PredefinedProductAttributeValueDto>();
}

public class PredefinedProductAttributeValueDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public double PriceAdjustment { get; set; }
    public double WeightAdjustment { get; set; }
    public double Cost { get; set; }
    public bool IsPreSelected { get; set; }
    public int DisplayOrder { get; set; }
}