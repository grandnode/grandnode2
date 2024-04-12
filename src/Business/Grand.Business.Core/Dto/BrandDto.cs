namespace Grand.Business.Core.Dto;

public class BrandDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string SeName { get; set; }
    public string Picture { get; set; }
    public string Description { get; set; }
    public string BottomDescription { get; set; }
    public string BrandLayoutId { get; set; }
    public string MetaKeywords { get; set; }
    public string MetaDescription { get; set; }
    public string MetaTitle { get; set; }
    public int? PageSize { get; set; }
    public bool? AllowCustomersToSelectPageSize { get; set; }
    public string PageSizeOptions { get; set; }
    public bool? ShowOnHomePage { get; set; }
    public bool? IncludeInMenu { get; set; }
    public string Icon { get; set; }
    public int? DefaultSort { get; set; }
    public string ExternalId { get; set; }
    public bool? Published { get; set; }
    public int? DisplayOrder { get; set; }
}