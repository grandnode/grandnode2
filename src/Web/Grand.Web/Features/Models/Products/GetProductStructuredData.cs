using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Products;

public class GetProductStructuredData : IRequest<ProductStructuredDataModel>
{
    public required Product Product { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Customer Customer { get; set; }

    /// <summary>
    ///     The currency the offer's price is published in
    /// </summary>
    public required Currency Currency { get; set; }
}
