﻿using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Common;
using MediatR;

namespace Grand.Web.Features.Models.Common
{
    public class GetAddressModel : IRequest<AddressModel>
    {
        public AddressModel Model { get; set; }
        public Address Address { get; set; }
        public bool ExcludeProperties { get; set; }
        public Func<IList<Country>> LoadCountries { get; set; }
        public bool PrePopulateWithCustomerFields { get; set; }
        public Customer Customer { get; set; }
        public Language Language { get; set; }
        public Store Store { get; set; }
        public IList<CustomAttribute> OverrideAttributes { get; set; }
    }
}
