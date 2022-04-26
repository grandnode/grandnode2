using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.Web.Common.Controllers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Payments.PayPalStandard.Services;
using System.Globalization;

namespace Payments.PayPalStandard.Controllers
{

    public class PaymentPayPalStandardController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IPaypalHttpClient _paypalHttpClient;

        private readonly PaymentSettings _paymentSettings;
        private readonly PayPalStandardPaymentSettings _payPalStandardPaymentSettings;

        public PaymentPayPalStandardController(
            IWorkContext workContext,
            IPaymentService paymentService,
            IOrderService orderService,
            ILogger logger,
            IMediator mediator,
            IPaymentTransactionService paymentTransactionService,
            IPaypalHttpClient paypalHttpClient,
            PayPalStandardPaymentSettings payPalStandardPaymentSettings,
            PaymentSettings paymentSettings)
        {
            _workContext = workContext;
            _paymentService = paymentService;
            _orderService = orderService;
            _logger = logger;
            _mediator = mediator;
            _paymentTransactionService = paymentTransactionService;
            _paypalHttpClient = paypalHttpClient;
            _payPalStandardPaymentSettings = payPalStandardPaymentSettings;
            _paymentSettings = paymentSettings;
        }


        private string QueryString(string name)
        {
            if (StringValues.IsNullOrEmpty(HttpContext.Request.Query[name]))
                return default;

            return HttpContext.Request.Query[name].ToString();
        }

        public async Task<IActionResult> PDTHandler()
        {
            var tx = QueryString("tx");

            if (_paymentService.LoadPaymentMethodBySystemName("Payments.PayPalStandard") is not PayPalStandardPaymentProvider processor ||
                !processor.IsPaymentMethodActive(_paymentSettings))
                throw new GrandException("PayPal Standard module cannot be loaded");

            (var status, var values, var _) = await _paypalHttpClient.GetPdtDetails(tx);

            if (status)
            {
                values.TryGetValue("custom", out var orderNumber);
                Guid orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(orderNumber);
                }
                catch { }
                Order order = await _orderService.GetOrderByGuid(orderNumberGuid);
                if (order != null)
                {
                    var paymentTransaction = await _paymentTransactionService.GetByOrdeGuid(orderNumberGuid);

                    double mc_gross = 0;
                    try
                    {
                        mc_gross = double.Parse(values["mc_gross"], new CultureInfo("en-US"));
                    }
                    catch (Exception exc)
                    {
                        _ = _logger.Error("PayPal PDT. Error getting mc_gross", exc);
                    }

                    values.TryGetValue("payer_status", out var payer_status);
                    values.TryGetValue("payment_status", out var payment_status);
                    values.TryGetValue("pending_reason", out var pending_reason);
                    values.TryGetValue("mc_currency", out var mc_currency);
                    values.TryGetValue("txn_id", out var txn_id);
                    values.TryGetValue("payment_type", out var payment_type);
                    values.TryGetValue("payer_id", out var payer_id);
                    values.TryGetValue("receiver_id", out var receiver_id);
                    values.TryGetValue("invoice", out var invoice);
                    values.TryGetValue("payment_fee", out var payment_fee);

                    var sb = new StringBuilder();
                    sb.AppendLine("Paypal PDT:");
                    sb.AppendLine("mc_gross: " + mc_gross);
                    sb.AppendLine("Payer status: " + payer_status);
                    sb.AppendLine("Payment status: " + payment_status);
                    sb.AppendLine("Pending reason: " + pending_reason);
                    sb.AppendLine("mc_currency: " + mc_currency);
                    sb.AppendLine("txn_id: " + txn_id);
                    sb.AppendLine("payment_type: " + payment_type);
                    sb.AppendLine("payer_id: " + payer_id);
                    sb.AppendLine("receiver_id: " + receiver_id);
                    sb.AppendLine("invoice: " + invoice);
                    sb.AppendLine("payment_fee: " + payment_fee);

                    var newPaymentStatus = PaypalHelper.GetPaymentStatus(payment_status, pending_reason);
                    sb.AppendLine("New payment status: " + newPaymentStatus);

                    //order note
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = sb.ToString(),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });

                    //load settings for a chosen store scope
                    //validate order total
                    if (_payPalStandardPaymentSettings.PdtValidateOrderTotal && !Math.Round(mc_gross, 2).Equals(Math.Round(order.OrderTotal * order.CurrencyRate, 2)))
                    {
                        string errorStr = string.Format("PayPal PDT. Returned order total {0} doesn't equal order total {1}. Order# {2}.", mc_gross, order.OrderTotal * order.CurrencyRate, order.OrderNumber);
                        _ = _logger.Error(errorStr);

                        //order note
                        await _orderService.InsertOrderNote(new OrderNote {
                            Note = errorStr,
                            OrderId = order.Id,
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow
                        });

                        return RedirectToAction("Index", "Home", new { area = "" });
                    }

                    //mark order as paid
                    if (newPaymentStatus == PaymentStatus.Paid)
                    {
                        if (await _mediator.Send(new CanMarkPaymentTransactionAsPaidQuery() { PaymentTransaction = paymentTransaction }))
                        {
                            paymentTransaction.AuthorizationTransactionId = txn_id;
                            await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
                            await _mediator.Send(new MarkAsPaidCommand() { PaymentTransaction = paymentTransaction });
                        }
                    }
                }

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
            else
            {
                var custom = QueryString("custom");
                Guid orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(custom);
                }
                catch { }
                Order order = await _orderService.GetOrderByGuid(orderNumberGuid);
                if (order != null)
                {                    
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                }
                else
                    return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> IPNHandler()
        {
            string strRequest = string.Empty;
            using (var stream = new StreamReader(Request.Body))
            {
                strRequest = await stream.ReadToEndAsync();
            }
            if (_paymentService.LoadPaymentMethodBySystemName("Payments.PayPalStandard") is not PayPalStandardPaymentProvider processor ||
                !processor.IsPaymentMethodActive(_paymentSettings))
                throw new GrandException("PayPal Standard module cannot be loaded");

            var (success, values) = await _paypalHttpClient.VerifyIpn(strRequest);

            if (success)
            {
                #region values
                double mc_gross = 0;
                try
                {
                    mc_gross = double.Parse(values["mc_gross"], new CultureInfo("en-US"));
                }
                catch { }

                values.TryGetValue("payer_status", out var payer_status);
                values.TryGetValue("payment_status", out var payment_status);
                values.TryGetValue("pending_reason", out var pending_reason);
                values.TryGetValue("mc_currency", out var mc_currency);
                values.TryGetValue("txn_id", out var txn_id);
                values.TryGetValue("txn_type", out var txn_type);
                values.TryGetValue("rp_invoice_id", out var rp_invoice_id);
                values.TryGetValue("payment_type", out var payment_type);
                values.TryGetValue("payer_id", out var payer_id);
                values.TryGetValue("receiver_id", out var receiver_id);
                values.TryGetValue("invoice", out var invoice);
                values.TryGetValue("payment_fee", out var payment_fee);

                #endregion

                var sb = new StringBuilder();
                sb.AppendLine("Paypal IPN:");
                foreach (KeyValuePair<string, string> kvp in values)
                {
                    sb.AppendLine(kvp.Key + ": " + kvp.Value);
                }

                var newPaymentStatus = PaypalHelper.GetPaymentStatus(payment_status, pending_reason);
                sb.AppendLine("New payment status: " + newPaymentStatus);

                switch (txn_type)
                {
                    case "recurring_payment_profile_created":
                        //do nothing here
                        break;
                    case "recurring_payment":
                        //do nothing here
                        break;
                    default:
                        #region Standard payment
                        {
                            string orderNumber = string.Empty;
                            values.TryGetValue("custom", out orderNumber);
                            Guid orderNumberGuid = Guid.Empty;
                            try
                            {
                                orderNumberGuid = new Guid(orderNumber);
                            }
                            catch
                            {
                            }

                            var order = await _orderService.GetOrderByGuid(orderNumberGuid);
                            if (order != null)
                            {
                                //order note
                                await _orderService.InsertOrderNote(new OrderNote {
                                    Note = sb.ToString(),
                                    DisplayToCustomer = false,
                                    CreatedOnUtc = DateTime.UtcNow,
                                    OrderId = order.Id,
                                });
                                var paymentTransaction = await _paymentTransactionService.GetByOrdeGuid(order.OrderGuid);

                                switch (newPaymentStatus)
                                {
                                    case PaymentStatus.Pending:
                                        {
                                        }
                                        break;
                                    case PaymentStatus.Authorized:
                                        {
                                            //validate order total
                                            if (Math.Round(mc_gross, 2).Equals(Math.Round(order.OrderTotal, 2)))
                                            {
                                                //valid
                                                if (await _mediator.Send(new CanMarkPaymentTransactionAsAuthorizedQuery() { PaymentTransaction = paymentTransaction }))
                                                {
                                                    await _mediator.Send(new MarkAsAuthorizedCommand() { PaymentTransaction = paymentTransaction });
                                                }
                                            }
                                            else
                                            {
                                                //not valid
                                                string errorStr = string.Format("PayPal IPN. Returned order total {0} doesn't equal order total {1}. Order# {2}.", mc_gross, order.OrderTotal * order.CurrencyRate, order.Id);
                                                //log
                                                _ = _logger.Error(errorStr);
                                                //order note
                                                await _orderService.InsertOrderNote(new OrderNote {
                                                    Note = errorStr,
                                                    DisplayToCustomer = false,
                                                    CreatedOnUtc = DateTime.UtcNow,
                                                    OrderId = order.Id,
                                                });
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Paid:
                                        {
                                            //validate order total
                                            if (Math.Round(mc_gross, 2).Equals(Math.Round(order.OrderTotal, 2)))
                                            {
                                                //valid
                                                if (await _mediator.Send(new CanMarkPaymentTransactionAsPaidQuery() { PaymentTransaction = paymentTransaction }))
                                                {
                                                    paymentTransaction.AuthorizationTransactionId = txn_id;
                                                    await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);

                                                    await _mediator.Send(new MarkAsPaidCommand() { PaymentTransaction = paymentTransaction });
                                                }
                                            }
                                            else
                                            {
                                                //not valid
                                                string errorStr = string.Format("PayPal IPN. Returned order total {0} doesn't equal order total {1}. Order# {2}.", mc_gross, order.OrderTotal * order.CurrencyRate, order.Id);
                                                //log
                                                _ = _logger.Error(errorStr);
                                                //order note
                                                await _orderService.InsertOrderNote(new OrderNote {
                                                    Note = errorStr,
                                                    DisplayToCustomer = false,
                                                    CreatedOnUtc = DateTime.UtcNow,
                                                    OrderId = order.Id,
                                                });
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Refunded:
                                        {
                                            var totalToRefund = Math.Abs(mc_gross);
                                            if (totalToRefund > 0 && Math.Round(totalToRefund, 2).Equals(Math.Round(order.OrderTotal, 2)))
                                            {
                                                //refund
                                                if (await _mediator.Send(new CanRefundOfflineQuery() { PaymentTransaction = paymentTransaction }))
                                                {
                                                    await _mediator.Send(new RefundOfflineCommand() { PaymentTransaction = paymentTransaction });
                                                }
                                            }
                                            else
                                            {
                                                //partial refund
                                                if (await _mediator.Send(new CanPartiallyRefundOfflineQuery() { PaymentTransaction = paymentTransaction, AmountToRefund = totalToRefund }))
                                                {
                                                    await _mediator.Send(new PartiallyRefundOfflineCommand() { PaymentTransaction = paymentTransaction, AmountToRefund = totalToRefund });
                                                }
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Voided:
                                        {
                                            if (await _mediator.Send(new CanVoidOfflineQuery() { PaymentTransaction = paymentTransaction }))
                                            {
                                                await _mediator.Send(new VoidOfflineCommand() { PaymentTransaction = paymentTransaction });
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                _ = _logger.Error("PayPal IPN. Order is not found", new GrandException(sb.ToString()));
                            }
                        }
                        #endregion
                        break;
                }
                return Ok();

            }
            else
            {
                _ = _logger.Error("PayPal IPN failed.", new GrandException(strRequest));
            }

            return BadRequest();

        }

        public async Task<IActionResult> CancelOrder()
        {
            var order = (await _orderService.SearchOrders(storeId: _workContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1)).FirstOrDefault();
            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("HomePage");
        }
    }
}