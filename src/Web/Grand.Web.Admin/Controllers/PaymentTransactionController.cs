using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.PaymentTransactions)]
public class PaymentTransactionController : BaseAdminController
{
    #region Constructors

    public PaymentTransactionController(
        ITranslationService translationService,
        IPaymentTransactionService paymentTransactionService,
        IOrderService orderService,
        IWorkContext workContext,
        IGroupService groupService,
        IDateTimeService dateTimeService,
        IMediator mediator
    )
    {
        _translationService = translationService;
        _paymentTransactionService = paymentTransactionService;
        _orderService = orderService;
        _workContext = workContext;
        _groupService = groupService;
        _dateTimeService = dateTimeService;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly IOrderService _orderService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;
    private readonly IGroupService _groupService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IMediator _mediator;

    #endregion Fields

    #region Methods

    //list
    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        var model = new PaymentTransactionListModel {
            PaymentTransactionStatus = TransactionStatus.Pending.ToSelectList(_translationService, _workContext, false)
                .ToList()
        };
        model.PaymentTransactionStatus.Insert(0,
            new SelectListItem
                { Text = _translationService.GetResource("Admin.Common.All"), Value = "-1", Selected = true });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, PaymentTransactionListModel model)
    {
        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            model.StoreId = _workContext.CurrentCustomer.StaffStoreId;
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone);

        Guid? orderGuid = null;
        if (!string.IsNullOrEmpty(model.OrderNumber))
            if (int.TryParse(model.OrderNumber, out var ordernumber))
            {
                var order = await _orderService.GetOrderByNumber(ordernumber);
                if (order != null)
                    orderGuid = order.OrderGuid;
            }

        var paymentTransactions = await _paymentTransactionService.SearchPaymentTransactions(
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
            var order = await _orderService.GetOrderByGuid(item.OrderGuid);
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
                CreatedOn = _dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc),
                TransactionStatus = item.TransactionStatus,
                Status = item.TransactionStatus.GetTranslationEnum(_translationService, _workContext)
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

        var order = await _orderService.GetOrderByNumber(id);
        if (order == null)
            return RedirectToAction("List", "PaymentTransaction");

        var paymentTransaction = await _paymentTransactionService.GetOrderByGuid(order.OrderGuid);
        if (paymentTransaction == null)
            //not found
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

        return RedirectToAction("Edit", "PaymentTransaction", new { id = paymentTransaction.Id });
    }


    //edit
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

        var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);

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
            CreatedOn = _dateTimeService.ConvertToUserTime(paymentTransaction.CreatedOnUtc, DateTimeKind.Utc),
            TransactionStatus = paymentTransaction.TransactionStatus,
            Status = paymentTransaction.TransactionStatus.GetTranslationEnum(_translationService, _workContext),
            IPAddress = paymentTransaction.IPAddress,
            Description = paymentTransaction.Description,
            AdditionalInfo = paymentTransaction.AdditionalInfo,
            AuthorizationTransactionId = paymentTransaction.AuthorizationTransactionId,
            //payment method buttons
            //model.CanCancelOrder = await _mediator.Send(new CanCancelOrderQuery() { Order = order });
            CanCapture = await _mediator.Send(new CanCaptureQuery { PaymentTransaction = paymentTransaction }),
            CanMarkAsPaid = await _mediator.Send(new CanMarkPaymentTransactionAsPaidQuery
                { PaymentTransaction = paymentTransaction }),
            CanRefund = await _mediator.Send(new CanRefundQuery { PaymentTransaction = paymentTransaction }),
            CanRefundOffline = await _mediator.Send(new CanRefundOfflineQuery
                { PaymentTransaction = paymentTransaction }),
            CanPartiallyRefund = await _mediator.Send(new CanPartiallyRefundQuery
                { PaymentTransaction = paymentTransaction, AmountToRefund = 0 }),
            CanPartiallyRefundOffline = await _mediator.Send(new CanPartiallyRefundOfflineQuery
                { PaymentTransaction = paymentTransaction, AmountToRefund = 0 }),
            CanPartiallyPaidOffline = await _mediator.Send(new CanPartiallyPaidOfflineQuery
                { PaymentTransaction = paymentTransaction, AmountToPaid = 0 }),
            CanVoid = await _mediator.Send(new CanVoidQuery { PaymentTransaction = paymentTransaction }),
            CanVoidOffline = await _mediator.Send(new CanVoidOfflineQuery { PaymentTransaction = paymentTransaction }),
            MaxAmountToRefund = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount,
            MaxAmountToPaid = paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount
        };

        return View(model);
    }


    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CapturePaymentTransaction(string id)
    {
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

        try
        {
            var errors = await _mediator.Send(new CaptureCommand { PaymentTransaction = paymentTransaction });

            foreach (var error in errors)
                Error(error);

            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MarkPaymentTransactionAsPaid(string id)
    {
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

        try
        {
            await _mediator.Send(new MarkAsPaidCommand { PaymentTransaction = paymentTransaction });
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RefundPaymentTransaction(string id)
    {
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

        try
        {
            var errors = await _mediator.Send(new RefundCommand { PaymentTransaction = paymentTransaction });
            foreach (var error in errors)
                Error(error);

            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RefundPaymentTransactionOffline(string id)
    {
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

        try
        {
            await _mediator.Send(new RefundOfflineCommand { PaymentTransaction = paymentTransaction });
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> VoidPaymentTransaction(string id)
    {
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

        try
        {
            var errors = await _mediator.Send(new VoidCommand { PaymentTransaction = paymentTransaction });
            foreach (var error in errors)
                Error(error);

            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> VoidPaymentTransactionOffline(string id)
    {
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

        try
        {
            await _mediator.Send(new VoidOfflineCommand { PaymentTransaction = paymentTransaction });
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc, false);
            return RedirectToAction("Edit", "PaymentTransaction", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> PartiallyRefundPopup(string id, bool online)
    {
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

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
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

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
                errors = (await _mediator.Send(new PartiallyRefundCommand
                    { PaymentTransaction = paymentTransaction, AmountToRefund = amountToRefund })).ToList();
            else
                await _mediator.Send(new PartiallyRefundOfflineCommand
                    { PaymentTransaction = paymentTransaction, AmountToRefund = amountToRefund });

            if (errors.Count == 0)
            {
                //success
                ViewBag.RefreshPage = true;
                return View(model);
            }

            //error
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
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

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
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List", "PaymentTransaction");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "PaymentTransaction");

        try
        {
            var amountToPaid = model.AmountToPaid;
            if (amountToPaid <= 0)
                throw new GrandException("Enter amount to refund");

            var maxAmountToPaid = paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount;
            if (amountToPaid > maxAmountToPaid)
                amountToPaid = maxAmountToPaid;

            await _mediator.Send(new PartiallyPaidOfflineCommand
                { PaymentTransaction = paymentTransaction, AmountToPaid = amountToPaid });

            ViewBag.RefreshPage = true;
            return View(model);
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return View(model);
        }
    }

    //delete
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var paymentTransaction = await _paymentTransactionService.GetById(id);
        if (paymentTransaction == null)
            return RedirectToAction("List");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer) &&
            paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("List", "MerchandiseReturn");
        if (ModelState.IsValid)
        {
            await _paymentTransactionService.DeletePaymentTransaction(paymentTransaction);
            Success(_translationService.GetResource("Admin.Orders.PaymentTransaction.Deleted"));
            return RedirectToAction("List", "PaymentTransaction");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = paymentTransaction.Id });
    }

    #endregion
}