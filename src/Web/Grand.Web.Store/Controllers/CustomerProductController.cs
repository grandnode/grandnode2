using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Domain.Customers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Grand.Web.Store.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AuthorizeStore]
[Area(Constants.AreaStore)]
[AuthorizeMenu]
[PerStoreCustomers]
public class CustomerProductController(
    ICustomerService customerService,
    ICustomerProductViewModelService customerProductViewModelService,
    ICustomerProductService customerProductService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerProductController(customerService, customerProductViewModelService, customerProductService, scope);
