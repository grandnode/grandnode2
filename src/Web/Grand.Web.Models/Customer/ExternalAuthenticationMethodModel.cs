using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Customer;

public class ExternalAuthenticationMethodModel : BaseModel
{
    public required string ViewComponentName { get; set; }
}