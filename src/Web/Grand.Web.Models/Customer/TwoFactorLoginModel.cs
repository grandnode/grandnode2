using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Grand.Web.Models.Customer;

public class TwoFactorLoginModel : BaseModel, IStoreAccessModel
{
    [BindNever]
    public StoreAccess StoreAccess { get; set; }
}
