namespace Grand.Business.Core.Dto;

public class ProductAttributeDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string SeName { get; set; }
    public string Description { get; set; }

    /// <summary>Predefined values, merged by name (case-insensitive): missing values are added, provided fields updated, none deleted</summary>
    public IList<ProductAttributeValueDto> Values { get; set; }
}

public class ProductAttributeValueDto
{
    public string Name { get; set; }
    public double? PriceAdjustment { get; set; }
    public double? WeightAdjustment { get; set; }
    public double? Cost { get; set; }
    public bool? IsPreSelected { get; set; }
    public int? DisplayOrder { get; set; }
}
