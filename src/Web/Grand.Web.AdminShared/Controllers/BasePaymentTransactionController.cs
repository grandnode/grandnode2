using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Permissions;
using Grand.Mediator;
using Grand.SharedKernel;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.PaymentTransactions)]
public abstract class BasePaymentTransactionController(
    IPaymentTransactionService paymentTransactionService,
    IOrderService orderService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<PaymentTransaction> scope)
    : BaseController
{
    // Exposed for host-specific concrete subclasses (none currently need extra actions, unlike
    // Shipment's Admin-only EditUserFields, but the accessors are added for consistency with
    // every other Base*Controller and so Task 3/4's own file sections can reference them).
    protected IPaymentTransactionService PaymentTransactionService => paymentTransactionService;
    protected IOrderService OrderService => orderService;
    protected ITranslationService TranslationService => translationService;
    protected IDateTimeService DateTimeService => dateTimeService;
    protected IMediator Mediator => mediator;
    protected IEnumTranslationService EnumTranslationService => enumTranslationService;
    protected IAdminDataScope<PaymentTransaction> Scope => scope;

    /// <summary>DRY replacement for the repeated "load payment transaction, redirect to List if not
    /// found or not authorized" pattern found in both original controllers (Admin never denied;
    /// Store denied on StoreId mismatch at every single action). Not a behavior change — every call
    /// site below still individually returns RedirectToAction("List") exactly as the originals
    /// did.</summary>
    protected async Task<(PaymentTransaction paymentTransaction, IActionResult denied)> LoadAuthorizedPaymentTransaction(string id)
    {
        var paymentTransaction = await paymentTransactionService.GetById(id);
        if (paymentTransaction == null) return (null, RedirectToAction("List", "PaymentTransaction"));
        if (!await scope.HasAccess(paymentTransaction)) return (null, RedirectToAction("List", "PaymentTransaction"));
        return (paymentTransaction, null);
    }

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        var model = new PaymentTransactionListModel {
            PaymentTransactionStatus = enumTranslationService.ToSelectList(TransactionStatus.Pending, false)
                .ToList()
        };
        model.PaymentTransactionStatus.Insert(0,
            new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "-1", Selected = true });
        return View(model);
    }

    /// <summary>Store's original forced <c>model.StoreId = StaffStoreId</c> unconditionally at the
    /// top of this action, overwriting whatever the posted grid model carried — not the usual
    /// "default when null" forcing pattern, because the List grid never lets a Store user pick a
    /// different store to begin with (there's no store selector on this screen for Store; compare
    /// Order's list, which does have one). Admin's original had no such line at all. Reproduced here
    /// as an unconditional assignment gated on <c>scope.DefaultStoreId</c> being non-null, which is
    /// exactly true for Store and exactly false (null) for Admin — same observable behavior as
    /// both originals, expressed once.</summary>
    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, PaymentTransactionListModel model)
    {
        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;

        DateTime? startDateValue = model.StartDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.StartDate.Value, dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.EndDate.Value, dateTimeService.CurrentTimeZone);

        Guid? orderGuid = null;
        if (!string.IsNullOrEmpty(model.OrderNumber))
            if (int.TryParse(model.OrderNumber, out var ordernumber))
            {
                var order = await orderService.GetOrderByNumber(ordernumber);
                if (order != null)
                    orderGuid = order.OrderGuid;
            }

        var paymentTransactions = await paymentTransactionService.SearchPaymentTransactions(
            customerEmail: model.SearchCustomerEmail,
            ts: model.SearchTransactionStatus >= 0 ? (TransactionStatus)model.SearchTransactionStatus : null,
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue,
            storeId: model.StoreId,
            orderGuid: orderGuid,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var dataModel = new List<PaymentTransactionModel>();

        foreach (var item in paymentTransactions)
        {
            var order = await orderService.GetOrderByGuid(item.OrderGuid);
            var trmodel = new PaymentTransactionModel {
                Id = item.Id,
                OrderCode = item.OrderCode,
                CustomerEmail = item.CustomerEmail,
                CustomerId = item.CustomerId,
                CurrencyCode = item.CurrencyCode,
                TransactionAmount = item.TransactionAmount,
                PaidAmount = item.PaidAmount,
                PaymentMethodSystemName = item.PaymentMethodSystemName,
                RefundedAmount = item.RefundedAmount,
                OrderId = order?.Id,
                OrderNumber = order?.OrderNumber,
                CreatedOn = dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc),
                TransactionStatus = item.TransactionStatus,
                Status = enumTranslationService.GetTranslationEnum(item.TransactionStatus)
            };
            dataModel.Add(trmodel);
        }

        var gridModel = new DataSourceResult {
            Data = dataModel.ToList(),
            Total = paymentTransactions.TotalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToOrderNumber(PaymentTransactionListModel model)
    {
        if (model.OrderNumber == null)
            return RedirectToAction("List", "PaymentTransaction");

        int.TryParse(model.OrderNumber, out var id);

        var order = await orderService.GetOrderByNumber(id);
        if (order == null)
            return RedirectToAction("List", "PaymentTransaction");

        var paymentTransaction = await paymentTransactionService.GetOrderByGuid(order.OrderGuid);
        if (paymentTransaction == null)
            //not found
            return RedirectToAction("List", "PaymentTransaction");

        if (!await scope.HasAccess(paymentTransaction))
            return RedirectToAction("List", "PaymentTransaction");

        return RedirectToAction("Edit", "PaymentTransaction", new { id = paymentTransaction.Id });
    }

    #endregion

    #region Edit

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        var order = await orderService.GetOrderByGuid(paymentTransaction.OrderGuid);

        var model = new PaymentTransactionModel {
            Id = paymentTransaction.Id,
            OrderCode = paymentTransaction.OrderCode,
            CustomerEmail = string.IsNullOrEmpty(paymentTransaction.CustomerEmail)
                ? "(null)"
                : paymentTransaction.CustomerEmail,
            CustomerId = paymentTransaction.CustomerId,
            CurrencyCode = paymentTransaction.CurrencyCode,
            TransactionAmount = paymentTransaction.TransactionAmount,
            PaidAmount = paymentTransaction.PaidAmount,
            PaymentMethodSystemName = paymentTransaction.PaymentMethodSystemName,
            RefundedAmount = paymentTransaction.RefundedAmount,
            OrderId = order?.Id,
            OrderNumber = order?.OrderNumber,
            CreatedOn = dateTimeService.ConvertToUserTime(paymentTransaction.CreatedOnUtc, DateTimeKind.Utc),
            TransactionStatus = paymentTransaction.TransactionStatus,
            Status = enumTranslationService.GetTranslationEnum(paymentTransaction.TransactionStatus),
            IPAddress = paymentTransaction.IPAddress,
            Description = paymentTransaction.Description,
            AdditionalInfo = paymentTransaction.AdditionalInfo,
            AuthorizationTransactionId = paymentTransaction.AuthorizationTransactionId,
            CanCapture = await mediator.Send(new CanCaptureQuery { PaymentTransaction = paymentTransaction }),
            CanMarkAsPaid = await mediator.Send(new CanMarkPaymentTransactionAsPaidQuery { PaymentTransaction = paymentTransaction }),
            CanRefund = await mediator.Send(new CanRefundQuery { PaymentTransaction = paymentTransaction }),
            CanRefundOffline = await mediator.Send(new CanRefundOfflineQuery { PaymentTransaction = paymentTransaction }),
            CanPartiallyRefund = await mediator.Send(new CanPartiallyRefundQuery { PaymentTransaction = paymentTransaction, AmountToRefund = 0 }),
            CanPartiallyRefundOffline = await mediator.Send(new CanPartiallyRefundOfflineQuery { PaymentTransaction = paymentTransaction, AmountToRefund = 0 }),
            CanPartiallyPaidOffline = await mediator.Send(new CanPartiallyPaidOfflineQuery { PaymentTransaction = paymentTransaction, AmountToPaid = 0 }),
            CanVoid = await mediator.Send(new CanVoidQuery { PaymentTransaction = paymentTransaction }),
            CanVoidOffline = await mediator.Send(new CanVoidOfflineQuery { PaymentTransaction = paymentTransaction }),
            MaxAmountToRefund = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount,
            MaxAmountToPaid = paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount
        };

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CapturePaymentTransaction(string id)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        try
        {
            var errors = await mediator.Send(new CaptureCommand { PaymentTransaction = paymentTransaction });

            foreach (var error in errors)
                Error(error);

            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MarkPaymentTransactionAsPaid(string id)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        try
        {
            await mediator.Send(new MarkAsPaidCommand { PaymentTransaction = paymentTransaction });
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RefundPaymentTransaction(string id)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        try
        {
            var errors = await mediator.Send(new RefundCommand { PaymentTransaction = paymentTransaction });
            foreach (var error in errors)
                Error(error);

            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RefundPaymentTransactionOffline(string id)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        try
        {
            await mediator.Send(new RefundOfflineCommand { PaymentTransaction = paymentTransaction });
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> VoidPaymentTransaction(string id)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        try
        {
            var errors = await mediator.Send(new VoidCommand { PaymentTransaction = paymentTransaction });
            foreach (var error in errors)
                Error(error);

            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> VoidPaymentTransactionOffline(string id)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        try
        {
            await mediator.Send(new VoidOfflineCommand { PaymentTransaction = paymentTransaction });
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    #endregion

    #region Partial refund / paid popups

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> PartiallyRefundPopup(string id, bool online)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        var model = new PaymentTransactionModel {
            Id = paymentTransaction.Id,
            MaxAmountToRefund = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount,
            CurrencyCode = paymentTransaction.CurrencyCode
        };

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PartiallyRefundPopup(string id, bool online, PaymentTransactionModel model)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        try
        {
            var amountToRefund = model.AmountToRefund;
            if (amountToRefund <= 0)
                throw new GrandException("Enter amount to refund");

            var maxAmountToRefund = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount;
            if (amountToRefund > maxAmountToRefund)
                amountToRefund = maxAmountToRefund;

            var errors = new List<string>();
            if (online)
                errors = (await mediator.Send(new PartiallyRefundCommand { PaymentTransaction = paymentTransaction, AmountToRefund = amountToRefund })).ToList();
            else
                await mediator.Send(new PartiallyRefundOfflineCommand { PaymentTransaction = paymentTransaction, AmountToRefund = amountToRefund });

            if (errors.Count == 0)
            {
                //success — Model.RefreshPage, read by both hosts' PartiallyRefundPopup.cshtml script
                //block after this task's view migration (Task 5). Admin's pre-consolidation
                //controller set ViewBag.RefreshPage here instead, which its own view never read
                //(a dead write, since the view already checked Model.RefreshPage) — deliberate bug
                //fix, unifying onto Store's already-correct behavior. See spec §2.
                model.RefreshPage = true;
                return View(model);
            }

            foreach (var error in errors)
                Error(error);

            return View(model);
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return View(model);
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> PartiallyPaidPopup(string id, bool online)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        var model = new PaymentTransactionModel {
            Id = paymentTransaction.Id,
            MaxAmountToPaid = paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount,
            CurrencyCode = paymentTransaction.CurrencyCode
        };

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PartiallyPaidPopup(string id, bool online, PaymentTransactionModel model)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        try
        {
            var amountToPaid = model.AmountToPaid;
            if (amountToPaid <= 0)
                throw new GrandException("Enter amount to refund");

            var maxAmountToPaid = paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount;
            if (amountToPaid > maxAmountToPaid)
                amountToPaid = maxAmountToPaid;

            await mediator.Send(new PartiallyPaidOfflineCommand { PaymentTransaction = paymentTransaction, AmountToPaid = amountToPaid });

            //same RefreshPage fix as PartiallyRefundPopup above.
            model.RefreshPage = true;
            return View(model);
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return View(model);
        }
    }

    #endregion

    #region Delete

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var (paymentTransaction, denied) = await LoadAuthorizedPaymentTransaction(id);
        if (denied != null) return denied;

        if (ModelState.IsValid)
        {
            await paymentTransactionService.DeletePaymentTransaction(paymentTransaction);
            Success(translationService.GetResource("Admin.Orders.PaymentTransaction.Deleted"));
            return RedirectToAction("List", "PaymentTransaction");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = paymentTransaction.Id });
    }

    #endregion
}
