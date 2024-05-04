using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Payments.BrainTree.Models;

public class PaymentInfoModel : BaseModel
{
    [GrandResourceDisplayName("Payment.SelectCreditCard")]
    public string CreditCardType { get; set; }

    [GrandResourceDisplayName("Payment.CardholderName")]

    public string CardholderName { get; set; }

    [GrandResourceDisplayName("Payment.CardNumber")]
    public string CardNumber { get; set; }

    [GrandResourceDisplayName("Payment.ExpirationDate")]
    public string ExpireMonth { get; set; }

    [GrandResourceDisplayName("Payment.ExpirationDate")]
    public string ExpireYear { get; set; }

    public IList<SelectListItem> ExpireMonths { get; set; } = new List<SelectListItem>();

    public IList<SelectListItem> ExpireYears { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("Payment.CardCode")]
    public string CardCode { get; set; }

    public string CardNonce { get; set; }
    public string Errors { get; set; }
}