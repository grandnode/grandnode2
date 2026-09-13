using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Customers;

public class CustomerAttributeModel : BaseEntityModel, ILocalizedModel<CustomerAttributeLocalizedModel>,
    IStoreLinkModel
{
    [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Fields.IsRequired")]
    public bool IsRequired { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Fields.IsReadOnly")]
    public bool IsReadOnly { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Fields.AttributeControlType")]
    public int AttributeControlTypeId { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Fields.AttributeControlType")]

    public string AttributeControlTypeName { get; set; }

    [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    //Store acl
    [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Fields.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this attribute is shared across all stores
    /// (i.e., not limited to the current store). Marked as read-only in store scope and
    /// calculated from <see cref="Stores"/> membership. Only relevant in store-scoped contexts.
    /// </summary>
    public bool IsGlobalAttribute { get; set; }

    public IList<CustomerAttributeLocalizedModel> Locales { get; set; } = new List<CustomerAttributeLocalizedModel>();
}

public class CustomerAttributeLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}