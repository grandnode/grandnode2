using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Orders;

/// <summary>
///     The form a guest asks for the link to return items of an order with
/// </summary>
public class GuestMerchandiseReturnModel : BaseModel
{
    [GrandResourceDisplayName("MerchandiseReturns.Guest.OrderCode")]
    public string OrderCode { get; set; }

    [GrandResourceDisplayName("MerchandiseReturns.Guest.Email")]
    public string Email { get; set; }

    public bool DisplayCaptcha { get; set; }
    public ICaptchaValidModel Captcha { get; set; } = new CaptchaModel();

    public string Result { get; set; }
}
