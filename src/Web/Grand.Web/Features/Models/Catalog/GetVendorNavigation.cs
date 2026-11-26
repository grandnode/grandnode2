using Grand.Domain.Localization;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Catalog;

public class GetVendorNavigation : IRequest<VendorNavigationModel>
{
    public required Language Language { get; set; }
}