using Grand.Domain.Common;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grand.Business.Common.Interfaces.Security;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetAddressListHandler : IRequestHandler<GetAddressList, CustomerAddressListModel>
    {
        private readonly ICountryService _countryService;
        private readonly IAclService _aclService;
        private readonly IMediator _mediator;
        public GetAddressListHandler(ICountryService countryService,
            IAclService aclService,
            IMediator mediator)
        {
            _countryService = countryService;
            _aclService = aclService;
            _mediator = mediator;
        }

        public async Task<CustomerAddressListModel> Handle(GetAddressList request, CancellationToken cancellationToken)
        {
            var model = new CustomerAddressListModel();
            var addresses = new List<Address>();
            foreach (var item in request.Customer.Addresses)
            {
                if (string.IsNullOrEmpty(item.CountryId))
                {
                    addresses.Add(item);
                    continue;
                }
                var country = await _countryService.GetCountryById(item.CountryId);
                if (country != null || _aclService.Authorize(country, request.Store.Id))
                {
                    addresses.Add(item);
                    continue;
                }
            }

            foreach (var address in addresses)
            {
                var countries = await _countryService.GetAllCountries(request.Language.Id, request.Store.Id);
                var addressModel = await _mediator.Send(new GetAddressModel() {
                    Language = request.Language,
                    Store = request.Store,
                    Model = null,
                    Address = address,
                    ExcludeProperties = false,
                    LoadCountries = () => countries
                });
                model.Addresses.Add(addressModel);
            }

            return model;
        }
    }
}
