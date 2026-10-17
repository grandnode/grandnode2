using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Tax;

namespace Grand.Web.Features.Models.ShoppingCart;

/// <summary>
///     Cart request that carries the values a calculation context is built from
/// </summary>
public interface ICartCalculationRequest
{
    Customer Customer { get; }
    Domain.Stores.Store Store { get; }
    Currency Currency { get; }
    TaxDisplayType TaxDisplayType { get; }
}
