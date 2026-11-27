using Grand.Domain.Localization;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Catalog;

public class GetVendorAll : IRequest<VendorListModel>
{
    public required Language Language { get; set; }
    public required VendorPagingModel Command { get; set; }
}