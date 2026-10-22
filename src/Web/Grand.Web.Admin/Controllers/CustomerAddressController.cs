using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class CustomerAddressController(
    ICustomerService customerService,
    ICustomerAddressViewModelService customerAddressViewModelService,
    IAddressAttributeParser addressAttributeParser,
    IAddressAttributeService addressAttributeService,
    IRequestTranslationService translationService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerAddressController(customerService, customerAddressViewModelService, addressAttributeParser, addressAttributeService, translationService, scope);
