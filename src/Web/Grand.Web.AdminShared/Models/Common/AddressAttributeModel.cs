using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Common;

public class AddressAttributeModel : BaseEntityModel, ILocalizedModel<AddressAttributeLocalizedModel>,
    IStoreLinkModel
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

    //Store acl
    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }

    /// <summary>
    /// True when this attribute is not exclusively assigned to the current host's single store.
    /// Always false for Admin (global scope has no notion of "my store"). Populated by
    /// BaseAddressAttributeController from IAdminDataScope&lt;AddressAttribute&gt;.DefaultStoreId,
    /// not persisted.
    /// </summary>
    public bool IsGlobalAttribute { get; set; }

    public IList<AddressAttributeLocalizedModel> Locales { get; set; } = new List<AddressAttributeLocalizedModel>();
}

public class AddressAttributeLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}