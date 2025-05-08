using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Commands.Models.Products;

public class InsertProductReviewCommand : IRequest<ProductReview>
{
    public Domain.Stores.Store Store { get; set; }
    public Customer Customer { get; set; }
    public Product Product { get; set; }
    public ProductReviewsModel Model { get; set; }
}