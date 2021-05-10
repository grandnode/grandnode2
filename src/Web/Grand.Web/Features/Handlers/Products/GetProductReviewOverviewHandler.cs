using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Products
{
    public class GetProductReviewOverviewHandler : IRequestHandler<GetProductReviewOverview, ProductReviewOverviewModel>
    {
        public GetProductReviewOverviewHandler()
        {
        }

        public async Task<ProductReviewOverviewModel> Handle(GetProductReviewOverview request, CancellationToken cancellationToken)
        {
            var productReview = new ProductReviewOverviewModel()
            {
                RatingSum = request.Product.ApprovedRatingSum,
                TotalReviews = request.Product.ApprovedTotalReviews,
                ProductId = request.Product.Id,
                AllowCustomerReviews = request.Product.AllowCustomerReviews
            };

            return await Task.FromResult(productReview);
        }
    }
}
