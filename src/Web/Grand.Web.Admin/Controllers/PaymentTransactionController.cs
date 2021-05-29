using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.PaymentTransactions)]
    public partial class PaymentTransactionController : BaseAdminController
    {
        #region Fields

        private readonly IPaymentService _paymentService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IOrderService _orderService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IMediator _mediator;

        #endregion Fields

        #region Constructors

        public PaymentTransactionController(
            IPaymentService paymentService,
            ITranslationService translationService,
            IPaymentTransactionService paymentTransactionService,
            IOrderService orderService,
            IWorkContext workContext,
            IGroupService groupService,
            IDateTimeService dateTimeService,
            IMediator mediator
            )
        {
            _paymentService = paymentService;
            _translationService = translationService;
            _paymentTransactionService = paymentTransactionService;
            _orderService = orderService;
            _workContext = workContext;
            _groupService = groupService;
            _dateTimeService = dateTimeService;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = new PaymentTransactionListModel
            {
                PaymentTransactionStatus = TransactionStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList()
            };
            model.PaymentTransactionStatus.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "-1", Selected = true });
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, PaymentTransactionListModel model)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;
            }
            DateTime? startDateValue = (model.StartDate == null) ? null
                : (DateTime?)_dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null) ? null
                : (DateTime?)_dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone);

            var paymentTransactions = await _paymentTransactionService.SearchPaymentTransactions(
                customerEmail: model.SearchCustomerEmail,
                ts: model.SearchTransactionStatus >= 0 ? (TransactionStatus)model.SearchTransactionStatus : null,
                createdFromUtc: startDateValue,
                createdToUtc: endDateValue,
                storeId: model.StoreId,
                pageIndex: command.Page - 1,
                pageSize: command.PageSize);

            var dataModel = new List<PaymentTransactionModel>();

            foreach (var item in paymentTransactions)
            {
                var order = await _orderService.GetOrderByGuid(item.OrderGuid);
                var trmodel = new PaymentTransactionModel();
                trmodel.Id = item.Id;
                trmodel.OrderCode = item.OrderCode;
                trmodel.CustomerEmail = item.CustomerEmail;
                trmodel.CustomerId = item.CustomerId;
                trmodel.CurrencyCode = item.CurrencyCode;
                trmodel.TransactionAmount = item.TransactionAmount;
                trmodel.PaidAmount = item.PaidAmount;
                trmodel.PaymentMethodSystemName = item.PaymentMethodSystemName;
                trmodel.RefundedAmount = item.RefundedAmount;
                trmodel.OrderId = order?.Id;
                trmodel.OrderNumber = order?.OrderNumber;
                trmodel.CreatedOn = _dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc);
                trmodel.TransactionStatus = item.TransactionStatus;
                trmodel.Status = item.TransactionStatus.GetTranslationEnum(_translationService, _workContext);
                dataModel.Add(trmodel);
            }

            var gridModel = new DataSourceResult
            {
                Data = dataModel.ToList(),
                Total = paymentTransactions.TotalCount,
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

            var paymentTransaction = await _paymentTransactionService.GetByOrdeGuid(order.OrderGuid);
            if (paymentTransaction == null)
                //not found
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
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

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);

            var model = new PaymentTransactionModel();
            model.Id = paymentTransaction.Id;
            model.OrderCode = paymentTransaction.OrderCode;
            model.CustomerEmail = paymentTransaction.CustomerEmail;
            model.CustomerId = paymentTransaction.CustomerId;
            model.CurrencyCode = paymentTransaction.CurrencyCode;
            model.TransactionAmount = paymentTransaction.TransactionAmount;
            model.PaidAmount = paymentTransaction.PaidAmount;
            model.PaymentMethodSystemName = paymentTransaction.PaymentMethodSystemName;
            model.RefundedAmount = paymentTransaction.RefundedAmount;
            model.OrderId = order?.Id;
            model.OrderNumber = order?.OrderNumber;
            model.CreatedOn = _dateTimeService.ConvertToUserTime(paymentTransaction.CreatedOnUtc, DateTimeKind.Utc);
            model.TransactionStatus = paymentTransaction.TransactionStatus;
            model.Status = paymentTransaction.TransactionStatus.GetTranslationEnum(_translationService, _workContext);
            model.IPAddress = paymentTransaction.IPAddress;
            model.Description = paymentTransaction.Description;
            model.AdditionalInfo = paymentTransaction.AdditionalInfo;


            //payment method buttons
            //model.CanCancelOrder = await _mediator.Send(new CanCancelOrderQuery() { Order = order });
            model.CanCapture = await _mediator.Send(new CanCaptureQuery() { PaymentTransaction = paymentTransaction });
            model.CanMarkAsPaid = await _mediator.Send(new CanMarkPaymentTransactionAsPaidQuery() { PaymentTransaction = paymentTransaction });
            model.CanRefund = await _mediator.Send(new CanRefundQuery() { PaymentTransaction = paymentTransaction });
            model.CanRefundOffline = await _mediator.Send(new CanRefundOfflineQuery() { PaymentTransaction = paymentTransaction });
            model.CanPartiallyRefund = await _mediator.Send(new CanPartiallyRefundQuery() { PaymentTransaction = paymentTransaction, AmountToRefund = 0 });
            model.CanPartiallyRefundOffline = await _mediator.Send(new CanPartiallyRefundOfflineQuery() { PaymentTransaction = paymentTransaction, AmountToRefund = 0 });
            model.CanPartiallyPaidOffline = await _mediator.Send(new CanPartiallyPaidOfflineQuery() { PaymentTransaction = paymentTransaction, AmountToPaid = 0 });
            model.CanVoid = await _mediator.Send(new CanVoidQuery() { PaymentTransaction = paymentTransaction });
            model.CanVoidOffline = await _mediator.Send(new CanVoidOfflineQuery() { PaymentTransaction = paymentTransaction });

            model.MaxAmountToRefund = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount;
            model.MaxAmountToPaid = paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount;

            return View(model);

        }


        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> CapturePaymentTransaction(string id)
        {
            var paymentTransaction = await _paymentTransactionService.GetById(id);
            if (paymentTransaction == null)
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            try
            {
                var errors = await _mediator.Send(new CaptureCommand() { PaymentTransaction = paymentTransaction });

                foreach (var error in errors)
                    Error(error, false);

                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, false);
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }

        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> MarkPaymentTransactionAsPaid(string id)
        {
            var paymentTransaction = await _paymentTransactionService.GetById(id);
            if (paymentTransaction == null)
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            try
            {
                await _mediator.Send(new MarkAsPaidCommand() { PaymentTransaction = paymentTransaction });
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, false);
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> RefundPaymentTransaction(string id)
        {
            var paymentTransaction = await _paymentTransactionService.GetById(id);
            if (paymentTransaction == null)
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            try
            {
                var errors = await _mediator.Send(new RefundCommand() { PaymentTransaction = paymentTransaction });
                foreach (var error in errors)
                    Error(error, false);
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, false);
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> RefundPaymentTransactionOffline(string id)
        {
            var paymentTransaction = await _paymentTransactionService.GetById(id);
            if (paymentTransaction == null)
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            try
            {
                await _mediator.Send(new RefundOfflineCommand() { PaymentTransaction = paymentTransaction });
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, false);
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> VoidPaymentTransaction(string id)
        {
            var paymentTransaction = await _paymentTransactionService.GetById(id);
            if (paymentTransaction == null)
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            try
            {
                var errors = await _mediator.Send(new VoidCommand() { PaymentTransaction = paymentTransaction });
                foreach (var error in errors)
                    Error(error, false);

                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, false);
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> VoidPaymentTransactionOffline(string id)
        {
            var paymentTransaction = await _paymentTransactionService.GetById(id);
            if (paymentTransaction == null)
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            try
            {
                await _mediator.Send(new VoidOfflineCommand() { PaymentTransaction = paymentTransaction });
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, false);
                return RedirectToAction("Edit", "PaymentTransaction", new { id = id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> PartiallyRefundPopup(string id, bool online)
        {
            var paymentTransaction = await _paymentTransactionService.GetById(id);
            if (paymentTransaction == null)
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            var model = new PaymentTransactionModel();
            model.Id = paymentTransaction.Id;
            model.MaxAmountToRefund = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount;
            model.CurrencyCode = paymentTransaction.CurrencyCode;

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> PartiallyRefundPopup(string id, bool online, PaymentTransactionModel model)
        {
            var paymentTransaction = await _paymentTransactionService.GetById(id);
            if (paymentTransaction == null)
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            try
            {
                double amountToRefund = model.AmountToRefund;
                if (amountToRefund <= 0)
                    throw new GrandException("Enter amount to refund");

                double maxAmountToRefund = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount;
                if (amountToRefund > maxAmountToRefund)
                    amountToRefund = maxAmountToRefund;

                var errors = new List<string>();
                if (online)
                    errors = (await _mediator.Send(new PartiallyRefundCommand { PaymentTransaction = paymentTransaction, AmountToRefund = amountToRefund })).ToList();
                else
                    await _mediator.Send(new PartiallyRefundOfflineCommand { PaymentTransaction = paymentTransaction, AmountToRefund = amountToRefund });

                if (errors.Count == 0)
                {
                    //success
                    ViewBag.RefreshPage = true;
                    return View(model);
                }
                //error
                foreach (var error in errors)
                    Error(error, false);
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

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            var model = new PaymentTransactionModel();
            model.Id = paymentTransaction.Id;
            model.MaxAmountToPaid = paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount;
            model.CurrencyCode = paymentTransaction.CurrencyCode;

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> PartiallyPaidPopup(string id, bool online, PaymentTransactionModel model)
        {
            var paymentTransaction = await _paymentTransactionService.GetById(id);
            if (paymentTransaction == null)
                return RedirectToAction("List", "PaymentTransaction");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "PaymentTransaction");
            }

            try
            {
                double amountToPaid = model.AmountToPaid;
                if (amountToPaid <= 0)
                    throw new GrandException("Enter amount to refund");

                double maxAmountToPaid = paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount;
                if (amountToPaid > maxAmountToPaid)
                    amountToPaid = maxAmountToPaid;

                await _mediator.Send(new PartiallyPaidOfflineCommand { PaymentTransaction = paymentTransaction, AmountToPaid = amountToPaid });

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

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && paymentTransaction.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "MerchandiseReturn");
            }

            if (_workContext.CurrentVendor != null)
                return RedirectToAction("List", "PaymentTransaction");

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
}
