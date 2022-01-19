using Grand.Domain.Localization;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Models.Catalog
{
    public class GetVendorAll : IRequest<IList<VendorModel>>
    {
        public Language Language { get; set; }
    }
}
