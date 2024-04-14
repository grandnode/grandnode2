using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Common;

public class AddressAttributeValueModel : BaseEntityModel, ILocalizedModel<AddressAttributeValueLocalizedModel>
{
    public string AddressAttributeId { get; set; }

    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Values.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Values.Fields.IsPreSelected")]
    public bool IsPreSelected { get; set; }

    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Values.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<AddressAttributeValueLocalizedModel> Locales { get; set; } =
        new List<AddressAttributeValueLocalizedModel>();
}

public class AddressAttributeValueLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Values.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}