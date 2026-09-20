using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class ShipmentController(
    IShipmentViewModelService shipmentViewModelService,
    IOrderService orderService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IShipmentService shipmentService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IAdminDataScope<Shipment> scope,
    IAdminDataScope<Order> orderScope)
    : BaseShipmentController(shipmentViewModelService, orderService, translationService,
        contextAccessor, pdfService, shipmentService, dateTimeService, mediator, scope, orderScope);
