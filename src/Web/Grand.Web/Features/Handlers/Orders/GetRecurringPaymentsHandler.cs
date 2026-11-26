using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Handlers.Orders;

public class GetRecurringPaymentsHandler : IRequestHandler<GetRecurringPayments, RecurringPaymentListModel>
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly IDateTimeService _dateTimeService;
    private readonly IEnumTranslationService _enumTranslationService;
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IRecurringPaymentService _recurringPaymentService;
    private readonly IRequestTranslationService _translationService;

    public GetRecurringPaymentsHandler(IRecurringPaymentService recurringPaymentService, IOrderService orderService,
        IProductService productService, IDateTimeService dateTimeService,
        IEnumTranslationService enumTranslationService, IRequestTranslationService translationService)
    {
        _recurringPaymentService = recurringPaymentService;
        _orderService = orderService;
        _productService = productService;
        _dateTimeService = dateTimeService;
        _enumTranslationService = enumTranslationService;
        _translationService = translationService;
    }

    public async Task<RecurringPaymentListModel> Handle(GetRecurringPayments request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Store);

        var pageIndex = Math.Max(request.Command.PageNumber, 1) - 1;
        var pageSize = request.Command.PageSize <= 0 ? DefaultPageSize : Math.Min(request.Command.PageSize, MaxPageSize);

        var recurringPayments = await _recurringPaymentService.GetCustomerRecurringPayments(request.Customer.Id,
            request.Store.Id, pageIndex, pageSize);

        var model = new RecurringPaymentListModel();
        model.PagingContext.LoadPagedList(recurringPayments);

        var orders = new Dictionary<string, Order>();
        var orderIds = recurringPayments.Select(x => x.InitialOrderId).OfType<string>().Where(x => x.Length > 0)
            .Distinct().ToArray();
        if (orderIds.Length > 0)
            foreach (var order in await _orderService.GetOrdersByIds(orderIds))
                orders[order.Id] = order;

        //a deleted order is not opened, so its products are not listed either
        var products = new Dictionary<string, Product>();
        var productIds = orders.Values.Where(x => !x.Deleted).SelectMany(x => x.OrderItems)
            .Select(x => x.ProductId).OfType<string>().Distinct().ToArray();
        if (productIds.Length > 0)
            foreach (var product in await _productService.GetProductsByIds(productIds, CatalogAccess.IncludingHidden()))
                products[product.Id] = product;

        foreach (var recurringPayment in recurringPayments)
        {
            var order = orders.GetValueOrDefault(recurringPayment.InitialOrderId ?? "");
            var viewableOrder = order is { Deleted: false } ? order : null;
            //the date the provider reported means nothing once it stopped charging
            var charging = recurringPayment.Status is RecurringPaymentStatus.Active or RecurringPaymentStatus.PastDue;

            model.RecurringPayments.Add(new RecurringPaymentListModel.RecurringPaymentRowModel {
                Id = recurringPayment.Id,
                CreatedOn = _dateTimeService.ConvertToUserTime(recurringPayment.CreatedOnUtc, DateTimeKind.Utc),
                StatusId = (int)recurringPayment.Status,
                Status = _enumTranslationService.GetTranslationEnum(recurringPayment.Status),
                CanCancel = !recurringPayment.Status.HasEnded(),
                IsPastDue = recurringPayment.Status == RecurringPaymentStatus.PastDue,
                Cycle = string.Format(
                    _translationService.GetResource(RecurringCycleText.ResourceKey(recurringPayment.CycleLength,
                        recurringPayment.CyclePeriodId)), recurringPayment.CycleLength),
                CyclesPaid = recurringPayment.TotalCycles > 0
                    ? string.Format(_translationService.GetResource("RecurringPayment.CyclesPaid.Of"),
                        recurringPayment.CyclesPaid, recurringPayment.TotalCycles)
                    : string.Format(_translationService.GetResource("RecurringPayment.CyclesPaid.UntilCancelled"),
                        recurringPayment.CyclesPaid),
                NextPaymentDate = charging && recurringPayment.NextPaymentDateUtc.HasValue
                    ? _dateTimeService.ConvertToUserTime(recurringPayment.NextPaymentDateUtc.Value, DateTimeKind.Utc)
                    : null,
                InitialOrderId = recurringPayment.InitialOrderId,
                InitialOrderNumber = order?.OrderNumber ?? 0,
                CanViewInitialOrder = viewableOrder != null,
                Products = viewableOrder?.OrderItems.Select(x => x.ProductId).OfType<string>().Where(products.ContainsKey)
                    .Select(id => products[id].GetTranslation(p => p.Name, request.Language.Id)).OfType<string>().ToList() ?? []
            });
        }

        return model;
    }
}
