using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Common;

public class AddressAttributeModel : BaseEntityModel, ILocalizedModel<AddressAttributeLocalizedModel>
{
    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.IsRequired")]
    public bool IsRequired { get; set; }

    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.AttributeControlType")]
    public int AttributeControlTypeId { get; set; }

    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.AttributeControlType")]

    public string AttributeControlTypeName { get; set; }

    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }


    public IList<AddressAttributeLocalizedModel> Locales { get; set; } = new List<AddressAttributeLocalizedModel>();
}

public class AddressAttributeLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}