using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Customers;
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
public class CustomerReviewController(
    ICustomerService customerService,
    IProductReviewService productReviewService,
    IProductReviewViewModelService productReviewViewModelService,
    IProductViewModelService productViewModelService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerReviewController(customerService, productReviewService, productReviewViewModelService, productViewModelService, scope);
