using Grand.Domain.Catalog;

namespace Grand.Business.Core.Dto;

/// <summary>
///     The variants of one existing product: attribute mappings with their values, and an explicit list of
///     combinations. Merged into what is stored; nothing is deleted
/// </summary>
public class ProductVariantsDto
{
    /// <summary>Product to change; when set and not found the row is rejected</summary>
    public string ProductId { get; set; }

    /// <summary>Used to find the product when ProductId is empty</summary>
    public string ProductSku { get; set; }

    public IList<ProductVariantAttributeDto> Attributes { get; set; }
    public IList<ProductVariantCombinationDto> Combinations { get; set; }
}

public class ProductVariantAttributeDto
{
    /// <summary>Catalogue product attribute by id or SeName; it must exist</summary>
    public string Attribute { get; set; }

    public AttributeControlType? ControlType { get; set; }
    public bool? IsRequired { get; set; }
    public bool? Combination { get; set; }
    public int? DisplayOrder { get; set; }

    /// <summary>Values of the mapping, merged by name (case-insensitive): missing values are added, provided fields updated</summary>
    public IList<ProductVariantValueDto> Values { get; set; }
}

public class ProductVariantValueDto
{
    public string Name { get; set; }
    public double? PriceAdjustment { get; set; }
    public double? WeightAdjustment { get; set; }
    public double? Cost { get; set; }
    public bool? IsPreSelected { get; set; }
    public string ColorSquaresRgb { get; set; }
    public int? DisplayOrder { get; set; }
}

public class ProductVariantCombinationDto
{
    /// <summary>Attribute (id or SeName) to value name; the set identifies the combination, order does not matter</summary>
    public IDictionary<string, string> Values { get; set; }

    public string Sku { get; set; }
    public string Gtin { get; set; }
    public string Mpn { get; set; }

    /// <summary>Overridden price of the combination</summary>
    public double? Price { get; set; }
}
