using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     Sales / Recurring payments in the Admin and Store panels. A recurring payment comes with an order, so the orders
///     permission covers it and the order's scope decides who sees it: the store panel its own store, a sales manager
///     the orders of their own customers
/// </summary>
[PermissionAuthorize(PermissionSystemName.Orders)]
[AutoValidateAntiforgeryToken]
public abstract class BaseRecurringPaymentController(
    IRecurringPaymentService recurringPaymentService,
    IOrderService orderService,
    ICustomerService customerService,
    IStoreService storeService,
    IPaymentService paymentService,
    IRequestTranslationService translationService,
    IEnumTranslationService enumTranslationService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IAdminDataScope<Order> scope,
    IContextAccessor contextAccessor)
    : BaseController
{
    protected sealed record AuthorizedRecurringPayment(RecurringPayment RecurringPayment, Order InitialOrder);

    /// <summary>A missing recurring payment and one the panel may not see are answered alike</summary>
    protected async Task<Access<AuthorizedRecurringPayment>> LoadAuthorizedRecurringPayment(string id)
    {
        var recurringPayment = await recurringPaymentService.GetRecurringPaymentById(id);
        if (recurringPayment == null) return Access<AuthorizedRecurringPayment>.Deny(RedirectToAction("List"));
        if (scope.DefaultStoreId is not null && recurringPayment.StoreId != scope.DefaultStoreId)
            return Access<AuthorizedRecurringPayment>.Deny(RedirectToAction("List"));

        //a deleted order is kept, so a missing one is a broken record and is shown to no one
        var initialOrder = string.IsNullOrEmpty(recurringPayment.InitialOrderId)
            ? null
            : await orderService.GetOrderById(recurringPayment.InitialOrderId);
        if (initialOrder == null || !await scope.HasAccess(initialOrder))
            return Access<AuthorizedRecurringPayment>.Deny(RedirectToAction("List"));

        return Access<AuthorizedRecurringPayment>.Allow(new AuthorizedRecurringPayment(recurringPayment, initialOrder));
    }

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        var model = new RecurringPaymentListModel {
            AvailableStatuses = enumTranslationService.ToSelectList(RecurringPaymentStatus.Pending, false).ToList(),
            ShowStoreFilter = scope.DefaultStoreId is null
        };
        model.AvailableStatuses.Insert(0,
            new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "-1", Selected = true });

        if (model.ShowStoreFilter)
        {
            model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var store in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = store.Shortcut, Value = store.Id });
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, RecurringPaymentListModel model)
    {
        var recurringPayments = await mediator.Send(new GetRecurringPaymentsQuery {
            StoreId = scope.DefaultStoreId ?? model.StoreId?.Trim(),
            CustomerEmail = model.SearchCustomerEmail,
            Status = model.SearchStatusId >= 0 ? (RecurringPaymentStatus)model.SearchStatusId : null,
            //the order list narrows a sales employee to their own orders the same way
            SalesEmployeeId = contextAccessor.WorkContext.CurrentCustomer.SeId,
            PageIndex = command.Page - 1,
            PageSize = command.PageSize
        });

        var orders = (await orderService.GetOrdersByIds(recurringPayments.Select(x => x.InitialOrderId)
                .OfType<string>().Where(x => x.Length > 0).Distinct().ToArray()))
            .ToDictionary(x => x.Id);
        var customers = (await customerService.GetCustomersByIds(recurringPayments.Select(x => x.CustomerId)
                .OfType<string>().Where(x => x.Length > 0).Distinct().ToArray()))
            .ToDictionary(x => x.Id);
        var stores = await storeService.GetAllStores();

        var data = recurringPayments.Select(x => PrepareRecurringPaymentModel(x,
            orders.GetValueOrDefault(x.InitialOrderId ?? ""), customers.GetValueOrDefault(x.CustomerId ?? ""),
            stores)).ToList();

        return Json(new DataSourceResult { Data = data, Total = recurringPayments.TotalCount });
    }

    #endregion

    #region Details

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var recurringPaymentAccess = await LoadAuthorizedRecurringPayment(id);
        if (recurringPaymentAccess.IsDenied) return recurringPaymentAccess.Denied;
        var (recurringPayment, initialOrder) = recurringPaymentAccess.Entity;

        var customer = await customerService.GetCustomerById(recurringPayment.CustomerId);
        var model = PrepareRecurringPaymentModel(recurringPayment, initialOrder, customer,
            await storeService.GetAllStores());
        model.ProviderReference = recurringPayment.ProviderReference;
        model.LastError = recurringPayment.LastError;
        model.LastErrorDate = recurringPayment.LastErrorDateUtc.HasValue
            ? dateTimeService.ConvertToUserTime(recurringPayment.LastErrorDateUtc.Value, DateTimeKind.Utc)
            : null;
        model.CancelledOn = recurringPayment.CancelledOnUtc.HasValue
            ? dateTimeService.ConvertToUserTime(recurringPayment.CancelledOnUtc.Value, DateTimeKind.Utc)
            : null;
        model.CancelledBy = recurringPayment.CancelledBy;

        var cycleOrders = (await orderService.GetOrdersByIds(recurringPayment.Cycles.Select(x => x.OrderId)
                .OfType<string>().Where(x => x.Length > 0).Distinct().ToArray()))
            .ToDictionary(x => x.Id);
        foreach (var cycle in recurringPayment.Cycles.OrderBy(x => x.CycleNumber))
        {
            var order = cycleOrders.GetValueOrDefault(cycle.OrderId ?? "");
            model.Cycles.Add(new RecurringPaymentModel.CycleModel {
                CycleNumber = cycle.CycleNumber,
                OrderId = order?.Id,
                OrderNumber = order?.OrderNumber ?? 0,
                CanViewOrder = order is { Deleted: false },
                //the gateway charges in the currency the customer ordered in
                Amount = $"{cycle.Amount:N2} {initialOrder.CustomerCurrencyCode}",
                PaidOn = dateTimeService.ConvertToUserTime(cycle.PaidOnUtc, DateTimeKind.Utc),
                ProviderReference = cycle.ProviderReference
            });
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Cancel)]
    [HttpPost]
    public async Task<IActionResult> Cancel(string id)
    {
        var recurringPaymentAccess = await LoadAuthorizedRecurringPayment(id);
        if (recurringPaymentAccess.IsDenied) return recurringPaymentAccess.Denied;
        var recurringPayment = recurringPaymentAccess.Entity.RecurringPayment;

        if (recurringPayment.Status.HasEnded())
        {
            Error(translationService.GetResource("Admin.Orders.RecurringPayments.Cancel.Ended"));
            return RedirectToAction("Edit", new { id });
        }

        var result = await mediator.Send(new CancelRecurringPaymentCommand {
            RecurringPayment = recurringPayment,
            CancelledBy = contextAccessor.WorkContext.CurrentCustomer.StatusChangedBy()
        });

        //staff see the provider's own words, they are the ones who can act on them
        if (result.Success)
            Success(translationService.GetResource("Admin.Orders.RecurringPayments.Cancel.Success"));
        else
            Error(string.Join(" ", result.Errors));

        return RedirectToAction("Edit", new { id });
    }

    #endregion

    private RecurringPaymentModel PrepareRecurringPaymentModel(RecurringPayment recurringPayment, Order? initialOrder,
        Customer? customer, IList<Store> stores)
    {
        //the date the provider reported means nothing once it stopped charging
        var charging = recurringPayment.Status is RecurringPaymentStatus.Active or RecurringPaymentStatus.PastDue;
        return new RecurringPaymentModel {
            Id = recurringPayment.Id,
            CustomerId = recurringPayment.CustomerId,
            CustomerEmail = customer?.Email ?? "",
            StoreName = scope.DefaultStoreId is null
                ? stores.FirstOrDefault(x => x.Id == recurringPayment.StoreId)?.Shortcut
                : null,
            StatusId = (int)recurringPayment.Status,
            Status = enumTranslationService.GetTranslationEnum(recurringPayment.Status),
            Cycle = string.Format(
                translationService.GetResource(RecurringCycleText.ResourceKey(recurringPayment.CycleLength,
                    recurringPayment.CyclePeriodId)), recurringPayment.CycleLength),
            CyclesPaid = recurringPayment.TotalCycles > 0
                ? string.Format(translationService.GetResource("RecurringPayment.CyclesPaid.Of"),
                    recurringPayment.CyclesPaid, recurringPayment.TotalCycles)
                : string.Format(translationService.GetResource("RecurringPayment.CyclesPaid.UntilCancelled"),
                    recurringPayment.CyclesPaid),
            NextPaymentDate = charging && recurringPayment.NextPaymentDateUtc.HasValue
                ? dateTimeService.ConvertToUserTime(recurringPayment.NextPaymentDateUtc.Value, DateTimeKind.Utc)
                : null,
            PaymentMethodName =
                paymentService.LoadPaymentMethodBySystemName(recurringPayment.PaymentMethodSystemName)?.FriendlyName ??
                recurringPayment.PaymentMethodSystemName,
            InitialOrderId = initialOrder?.Id,
            InitialOrderNumber = initialOrder?.OrderNumber ?? 0,
            CanViewInitialOrder = initialOrder is { Deleted: false },
            CreatedOn = dateTimeService.ConvertToUserTime(recurringPayment.CreatedOnUtc, DateTimeKind.Utc),
            CanCancel = !recurringPayment.Status.HasEnded()
        };
    }
}
