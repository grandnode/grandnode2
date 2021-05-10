using Grand.Domain.Localization;
using Grand.Domain.Vendors;
using Grand.Business.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Messages.Commands.Models
{
    public class GetVendorTokensCommand : IRequest<LiquidVendor>
    {
        public Vendor Vendor { get; set; }
        public Language Language { get; set; }
    }
}
