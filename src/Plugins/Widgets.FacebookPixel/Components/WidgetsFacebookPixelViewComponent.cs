using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.Threading.Tasks;
using Widgets.FacebookPixel.Models;

namespace Widgets.FacebookPixel.Components
{
    [ViewComponent(Name = "WidgetsFacebookPixel")]
    public class WidgetsFacebookPixelViewComponent : ViewComponent
    {
        private readonly IWorkContext _workContext;
        private readonly IOrderService _orderService;
        private readonly ICookiePreference _cookiePreference;
        private readonly FacebookPixelSettings _facebookPixelSettings;

        public WidgetsFacebookPixelViewComponent(
            IWorkContext workContext,
            IOrderService orderService,
            ICookiePreference cookiePreference,
            FacebookPixelSettings facebookPixelSettings
            )
        {
            _workContext = workContext;
            _orderService = orderService;
            _cookiePreference = cookiePreference;
            _facebookPixelSettings = facebookPixelSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
        {

            if (_facebookPixelSettings.AllowToDisableConsentCookie)
            {
                var enabled = await _cookiePreference.IsEnable(_workContext.CurrentCustomer, _workContext.CurrentStore, FacebookPixelDefaults.ConsentCookieSystemName);
                if ((enabled.HasValue && !enabled.Value) || (!enabled.HasValue && !_facebookPixelSettings.ConsentDefaultState))
                    return Content("");
            }
            //page
            if (widgetZone == FacebookPixelDefaults.Page)
            {
                return View("Default", GetTrackingScript());
            }
            //add to cart
            if (widgetZone == FacebookPixelDefaults.AddToCart)
            {
                var model = JsonConvert.DeserializeObject<FacebookAddToCartModelModel>(JsonConvert.SerializeObject(additionalData));
                if (model != null)
                {
                    return View("Default", GetAddToCartScript(model));
                }
            }
            //order details 
            if (widgetZone == FacebookPixelDefaults.OrderDetails)
            {
                var orderId = additionalData as string;
                if (!string.IsNullOrEmpty(orderId))
                {
                    return View("Default", await GetOrderScript(orderId));
                }

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
            trackingScript = trackingScript.Replace("{AMOUNT}", model.DecimalPrice.ToString("F2", CultureInfo.InvariantCulture));
            trackingScript = trackingScript.Replace("{CURRENCY}", _workContext.WorkingCurrency.CurrencyCode);
            return trackingScript;
        }

        private async Task<string> GetOrderScript(string orderId)
        {
            var trackingScript = _facebookPixelSettings.DetailsOrderScript + "\n";
            var order = await _orderService.GetOrderById(orderId);
            if (order != null)
            {
                trackingScript = trackingScript.Replace("{AMOUNT}", order.OrderTotal.ToString("F2", CultureInfo.InvariantCulture));
                trackingScript = trackingScript.Replace("{CURRENCY}", order.CustomerCurrencyCode);
            }
            return trackingScript;
        }
    }
}