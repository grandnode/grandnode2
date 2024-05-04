using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Catalog;

public class SpecificationAttributeOptionModel : BaseEntityModel,
    ILocalizedModel<SpecificationAttributeOptionLocalizedModel>
{
    public string SpecificationAttributeId { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.SeName")]
    public string SeName { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.ColorSquaresRgb")]
    public string ColorSquaresRgb { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.EnableColorSquaresRgb")]
    public bool EnableColorSquaresRgb { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [GrandResourceDisplayName(
        "Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.NumberOfAssociatedProducts")]
    public int NumberOfAssociatedProducts { get; set; }

    public IList<SpecificationAttributeOptionLocalizedModel> Locales { get; set; } =
        new List<SpecificationAttributeOptionLocalizedModel>();
}

public class SpecificationAttributeOptionLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}