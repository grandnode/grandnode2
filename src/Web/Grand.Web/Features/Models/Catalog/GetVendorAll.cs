using Grand.Domain.Localization;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Catalog;

public class GetVendorAll : IRequest<VendorListModel>
{
    public Language Language { get; set; }
    public VendorPagingModel Command { get; set; }
}