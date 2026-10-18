using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Vendors;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetVendorTokensCommand : IRequest<LiquidVendor>
{
    public Vendor Vendor { get; set; }
    public Language Language { get; set; }
}