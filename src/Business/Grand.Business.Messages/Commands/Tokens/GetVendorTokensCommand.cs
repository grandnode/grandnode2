using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Vendors;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetVendorTokensCommand : IRequest<LiquidVendor>
{
    public required Vendor Vendor { get; set; }
    public required Language Language { get; set; }
}