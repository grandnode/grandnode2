using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Core.Commands.Messages.Tokens;

public class GetVendorTokensCommand : IRequest<LiquidVendor>
{
    public Vendor Vendor { get; set; }
    public Language Language { get; set; }
}