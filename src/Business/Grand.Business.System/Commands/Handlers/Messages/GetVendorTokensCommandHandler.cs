using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Messages.Commands.Models;
using Grand.Business.Messages.DotLiquidDrops;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetVendorTokensCommandHandler : IRequestHandler<GetVendorTokensCommand, LiquidVendor>
    {
        private readonly ICountryService _countryService;

        public GetVendorTokensCommandHandler(
            ICountryService countryService)
        {
            _countryService = countryService;
        }

        public async Task<LiquidVendor> Handle(GetVendorTokensCommand request, CancellationToken cancellationToken)
        {
            var liquidVendor = new LiquidVendor(request.Vendor);
            var country = await _countryService.GetCountryById(request.Vendor.Address.CountryId);
            liquidVendor.StateProvince = !string.IsNullOrEmpty(request.Vendor.Address?.StateProvinceId) ?
                country?.StateProvinces.FirstOrDefault(x=>x.Id == request.Vendor.Address.StateProvinceId)?.GetTranslation(x => x.Name, request.Language.Id) : "";

            liquidVendor.Country = !string.IsNullOrEmpty(request.Vendor.Address?.CountryId) ?
                country?.GetTranslation(x => x.Name, request.Language.Id) : "";

            return liquidVendor;
        }
    }
}
