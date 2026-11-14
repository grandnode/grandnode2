using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Orders;

/// <summary>
///     The form a guest asks for the link to an order with
/// </summary>
public class GuestOrderModel : BaseModel
{
    [GrandResourceDisplayName("Order.Guest.OrderNumberOrCode")]
    public string OrderNumberOrCode { get; set; }

    [GrandResourceDisplayName("Order.Guest.Email")]
    public string Email { get; set; }

    public bool DisplayCaptcha { get; set; }
    public ICaptchaValidModel Captcha { get; set; } = new CaptchaModel();

    public string Result { get; set; }
}
