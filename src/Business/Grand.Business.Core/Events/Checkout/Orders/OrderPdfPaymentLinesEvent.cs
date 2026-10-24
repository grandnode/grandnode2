using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Events.Checkout.Orders;

/// <summary>
///     Published while an order PDF is composed. Lines a handler adds are printed below the payment method,
///     where the pdf_payment_method_below widget zone used to render.
/// </summary>
public class OrderPdfPaymentLinesEvent : INotification
{
    public OrderPdfPaymentLinesEvent(Order order, string languageId)
    {
        Order = order;
        LanguageId = languageId;
    }

    public Order Order { get; }

    /// <summary>
    ///     Language the PDF is written in
    /// </summary>
    public string LanguageId { get; }

    /// <summary>
    ///     Plain text lines; markup is not rendered
    /// </summary>
    public IList<string> Lines { get; } = new List<string>();
}
