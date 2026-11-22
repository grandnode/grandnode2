using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Grand.Web.Models.Customer;

public class OidcLinkModel : IStoreAccessModel
{
    [BindNever]
    public StoreAccess StoreAccess { get; set; }

    public string Email { get; set; }
    public string LoginUrl { get; set; }
}
