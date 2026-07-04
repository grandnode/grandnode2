using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.Store.Models.Customers;

public class CustomerAttributeStoreModel : CustomerAttributeModel
{
    public bool IsGlobalAttribute { get; set; }
}
