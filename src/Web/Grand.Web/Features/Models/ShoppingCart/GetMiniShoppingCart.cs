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
    public Customer Customer { get; set; }
    public Language Language { get; set; }
    public Currency Currency { get; set; }
    public Domain.Stores.Store Store { get; set; }

    /// <summary>
    ///     Store address of the request, used for the download links of product attributes
    /// </summary>
    public DomainHost Host { get; set; }
    public TaxDisplayType TaxDisplayType { get; set; }
}