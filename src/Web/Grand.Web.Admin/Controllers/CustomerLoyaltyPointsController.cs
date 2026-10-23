using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class CustomerLoyaltyPointsController(
    ICustomerService customerService,
    ICustomerLoyaltyPointsViewModelService customerLoyaltyPointsViewModelService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerLoyaltyPointsController(customerService, customerLoyaltyPointsViewModelService, scope);
