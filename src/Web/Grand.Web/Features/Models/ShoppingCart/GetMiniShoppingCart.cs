using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Web.Models.ShoppingCart;
using Grand.Mediator;

namespace Grand.Web.Features.Models.ShoppingCart;

public class GetMiniShoppingCart : IRequest<MiniShoppingCartModel>, ICartCalculationRequest
{
    public required Customer Customer { get; set; }
    public required Language Language { get; set; }
    public required Currency Currency { get; set; }
    public required Domain.Stores.Store Store { get; set; }

    /// <summary>
    ///     Store address of the request, used for the download links of product attributes
    /// </summary>
    public required DomainHost Host { get; set; }
    public TaxDisplayType TaxDisplayType { get; set; }
}