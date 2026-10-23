using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class OrderController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    IOrderStatusService orderStatusService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IMediator mediator,
    IAdminDataScope<Order> scope,
    IOrderNoteViewModelService orderNoteViewModelService,
    IOrderAddressViewModelService orderAddressViewModelService,
    IOrderProductViewModelService orderProductViewModelService)
    : BaseOrderManagementController(orderViewModelService, orderService, orderStatusService,
        translationService, contextAccessor, pdfService, mediator, scope, orderNoteViewModelService,
        orderAddressViewModelService, orderProductViewModelService);
