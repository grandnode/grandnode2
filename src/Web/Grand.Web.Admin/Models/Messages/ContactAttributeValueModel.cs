using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Messages;

public class ContactAttributeValueModel : BaseEntityModel, ILocalizedModel<ContactAttributeValueLocalizedModel>
{
    public string ContactAttributeId { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Values.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Values.Fields.ColorSquaresRgb")]
    public string ColorSquaresRgb { get; set; }

    public bool DisplayColorSquaresRgb { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Values.Fields.IsPreSelected")]
    public bool IsPreSelected { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Values.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<ContactAttributeValueLocalizedModel> Locales { get; set; } =
        new List<ContactAttributeValueLocalizedModel>();
}

public class ContactAttributeValueLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Values.Fields.Name")]
    public string Name { get; set; }

    public string LanguageId { get; set; }
}