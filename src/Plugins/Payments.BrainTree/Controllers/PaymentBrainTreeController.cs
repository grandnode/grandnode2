﻿using Braintree;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Payments.BrainTree.Models;
using Payments.BrainTree.Validators;
using System.Net;
using Environment = Braintree.Environment;

namespace Payments.BrainTree.Controllers;

public class PaymentBrainTreeController : BasePaymentController
{
    private readonly BrainTreePaymentSettings _brainTreePaymentSettings;
    private readonly IOrderCalculationService _orderTotalCalculationService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public PaymentBrainTreeController(BrainTreePaymentSettings brainTreePaymentSettings,
        IOrderCalculationService orderTotalCalculationService,
        IShoppingCartService shoppingCartService,
        IWorkContextAccessor workContextAccessor,
        ITranslationService translationService)
    {
        _brainTreePaymentSettings = brainTreePaymentSettings;
        _orderTotalCalculationService = orderTotalCalculationService;
        _shoppingCartService = shoppingCartService;
        _workContextAccessor = workContextAccessor;
        _translationService = translationService;
    }

    public async Task<IActionResult> PaymentInfo()
    {
        var model = new PaymentInfoModel();

        if (_brainTreePaymentSettings.Use3DS)
        {
            var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id,
                ShoppingCartType.ShoppingCart);

            if (!cart.Any())
                throw new Exception("Your cart is empty");

            //get settings
            var useSandBox = _brainTreePaymentSettings.UseSandBox;
            var merchantId = _brainTreePaymentSettings.MerchantId;
            var publicKey = _brainTreePaymentSettings.PublicKey;
            var privateKey = _brainTreePaymentSettings.PrivateKey;

            var gateway = new BraintreeGateway {
                Environment = useSandBox ? Environment.SANDBOX : Environment.PRODUCTION,
                MerchantId = merchantId,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };

            ViewBag.ClientToken = await gateway.ClientToken.GenerateAsync();
            ViewBag.OrderTotal = (await _orderTotalCalculationService.GetShoppingCartTotal(cart)).shoppingCartTotal;

            return View("PaymentInfo_3DS", model);
        }

        //years
        for (var i = 0; i < 15; i++)
        {
            var year = Convert.ToString(DateTime.Now.Year + i);
            model.ExpireYears.Add(new SelectListItem {
                Text = year,
                Value = year
            });
        }

        //months
        for (var i = 1; i <= 12; i++)
        {
            var text = i < 10 ? "0" + i : i.ToString();
            model.ExpireMonths.Add(new SelectListItem {
                Text = text,
                Value = i.ToString()
            });
        }

        //set postback values (we cannot access "Form" with "GET" requests)
        if (Request.Method == WebRequestMethods.Http.Get)
            return View("PaymentInfo", model);

        var form = await HttpContext.Request.ReadFormAsync();

        model.CardholderName = form["CardholderName"];
        model.CardNumber = form["CardNumber"];
        model.CardCode = form["CardCode"];

        var selectedMonth = model.ExpireMonths
            .FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
        if (selectedMonth != null)
            selectedMonth.Selected = true;
        var selectedYear = model.ExpireYears
            .FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
        if (selectedYear != null)
            selectedYear.Selected = true;

        var validator = new PaymentInfoValidator(_brainTreePaymentSettings, _translationService);
        var results = await validator.ValidateAsync(model);
        if (results.IsValid) return View("PaymentInfo", model);
        var query = from error in results.Errors
            select error.ErrorMessage;
        model.Errors = string.Join(", ", query);
        return View("PaymentInfo", model);
    }
}