﻿using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
using Widgets.FacebookPixel.Models;

namespace Widgets.FacebookPixel.Components;

[ViewComponent(Name = "WidgetsFacebookPixel")]
public class WidgetsFacebookPixelViewComponent : ViewComponent
{
    private readonly ICookiePreference _cookiePreference;
    private readonly FacebookPixelSettings _facebookPixelSettings;
    private readonly IOrderService _orderService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public WidgetsFacebookPixelViewComponent(
        IWorkContextAccessor workContextAccessor,
        IOrderService orderService,
        ICookiePreference cookiePreference,
        FacebookPixelSettings facebookPixelSettings
    )
    {
        _workContextAccessor = workContextAccessor;
        _orderService = orderService;
        _cookiePreference = cookiePreference;
        _facebookPixelSettings = facebookPixelSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
    {
        if (_facebookPixelSettings.AllowToDisableConsentCookie)
        {
            var enabled = await _cookiePreference.IsEnable(_workContextAccessor.WorkContext.CurrentCustomer, _workContextAccessor.WorkContext.CurrentStore,
                FacebookPixelDefaults.ConsentCookieSystemName);
            if ((enabled.HasValue && !enabled.Value) ||
                (!enabled.HasValue && !_facebookPixelSettings.ConsentDefaultState))
                return Content("");
        }

        //page
        if (widgetZone == FacebookPixelDefaults.Page) return View("Default", GetTrackingScript());
        //add to cart
        if (widgetZone == FacebookPixelDefaults.AddToCart)
        {
            var model =
                JsonSerializer.Deserialize<FacebookAddToCartModelModel>(JsonSerializer.Serialize(additionalData));
            if (model != null) return View("Default", GetAddToCartScript(model));
        }

        //order details 
        if (widgetZone == FacebookPixelDefaults.OrderDetails)
        {
            var orderId = additionalData as string;
            if (!string.IsNullOrEmpty(orderId)) return View("Default", await GetOrderScript(orderId));
        }

        return Content("");
    }

    private string GetTrackingScript()
    {
        var trackingScript = _facebookPixelSettings.PixelScript + "\n";
        trackingScript = trackingScript.Replace("{PIXELID}", _facebookPixelSettings.PixelId);
        return trackingScript;
    }

    private string GetAddToCartScript(FacebookAddToCartModelModel model)
    {
        var trackingScript = _facebookPixelSettings.AddToCartScript + "\n";
        trackingScript = trackingScript.Replace("{PRODUCTID}", model.ProductId);
        trackingScript = trackingScript.Replace("{PRODUCTNAME}", model.ProductName);
        trackingScript = trackingScript.Replace("{QTY}", model.Quantity.ToString("N0"));
        trackingScript =
            trackingScript.Replace("{AMOUNT}", model.DecimalPrice.ToString("F2", CultureInfo.InvariantCulture));
        trackingScript = trackingScript.Replace("{CURRENCY}", _workContextAccessor.WorkContext.WorkingCurrency.CurrencyCode);
        return trackingScript;
    }

    private async Task<string> GetOrderScript(string orderId)
    {
        var trackingScript = _facebookPixelSettings.DetailsOrderScript + "\n";
        var order = await _orderService.GetOrderById(orderId);
        if (order != null)
        {
            trackingScript =
                trackingScript.Replace("{AMOUNT}", order.OrderTotal.ToString("F2", CultureInfo.InvariantCulture));
            trackingScript = trackingScript.Replace("{CURRENCY}", order.CustomerCurrencyCode);
        }

        return trackingScript;
    }
}