using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Web.Models.ShoppingCart;
using Grand.Mediator;

namespace Grand.Web.Features.Models.ShoppingCart;

public class GetAddToCart : IRequest<AddToCartModel>, ICartCalculationRequest
{
    public required Product Product { get; set; }
    public required Customer Customer { get; set; }
    public required Language Language { get; set; }
    public required Currency Currency { get; set; }
    public required Domain.Stores.Store Store { get; set; }

    /// <summary>
    ///     Store address of the request, used for the download links of product attributes
    /// </summary>
    public required DomainHost Host { get; set; }
    public ShoppingCartItem? ShoppingCartItem { get; set; }
    public TaxDisplayType TaxDisplayType { get; set; }
    public int Quantity { get; set; }
    public double? CustomerEnteredPrice { get; set; }
    public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();
    public ShoppingCartType CartType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ReservationId { get; set; }
    public string? Parameter { get; set; }
    public string? Duration { get; set; }
}