using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class CustomerOrderController(
    ICustomerService customerService,
    IOrderService orderService,
    IOrderViewModelService orderViewModelService,
    IRequestPermissionService permissionService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerOrderController(customerService, orderService, orderViewModelService, permissionService, scope);
