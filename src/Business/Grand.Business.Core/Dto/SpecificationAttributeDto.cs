namespace Grand.Business.Core.Dto;

public class SpecificationAttributeDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string SeName { get; set; }
    public int? DisplayOrder { get; set; }

    /// <summary>Merged by name (case-insensitive): missing options are added, provided fields updated, none deleted</summary>
    public IList<SpecificationAttributeOptionDto> Options { get; set; }
}

public class SpecificationAttributeOptionDto
{
    public string Name { get; set; }
    public string ColorSquaresRgb { get; set; }
    public int? DisplayOrder { get; set; }
}
