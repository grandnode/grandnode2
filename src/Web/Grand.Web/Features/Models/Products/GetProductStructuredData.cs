using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Products;

public class GetProductStructuredData : IRequest<ProductStructuredDataModel>
{
    public Product Product { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public Customer Customer { get; set; }

    /// <summary>
    ///     The currency the offer's price is published in
    /// </summary>
    public Currency Currency { get; set; }
}
