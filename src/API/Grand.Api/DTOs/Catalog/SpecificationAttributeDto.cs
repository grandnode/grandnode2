using Grand.Api.Models;

namespace Grand.Api.DTOs.Catalog;

public class SpecificationAttributeDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public int DisplayOrder { get; set; }

    public IList<SpecificationAttributeOptionDto> SpecificationAttributeOptions { get; set; } =
        new List<SpecificationAttributeOptionDto>();
}

public class SpecificationAttributeOptionDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public int DisplayOrder { get; set; }
    public string ColorSquaresRgb { get; set; }
}