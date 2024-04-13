using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers;

public class GetReviewsHandler : IRequestHandler<GetReviews, CustomerProductReviewsModel>
{
    private readonly IDateTimeService _dateTimeService;
    private readonly IProductReviewService _productReviewService;
    private readonly IProductService _productService;
    private readonly ITranslationService _translationService;

    public GetReviewsHandler(
        ITranslationService translationService,
        IProductService productService,
        IProductReviewService productReviewService,
        IDateTimeService dateTimeService)
    {
        _translationService = translationService;
        _productService = productService;
        _productReviewService = productReviewService;
        _dateTimeService = dateTimeService;
    }

    public async Task<CustomerProductReviewsModel> Handle(GetReviews request, CancellationToken cancellationToken)
    {
        var reviewsModel = new CustomerProductReviewsModel {
            CustomerId = request.Customer.Id,
            CustomerInfo = request.Customer != null
                ? !string.IsNullOrEmpty(request.Customer.Email)
                    ? request.Customer.Email
                    : _translationService.GetResource("Admin.Customers.Guest")
                : ""
        };

        var productReviews = await _productReviewService.GetAllProductReviews(request.Customer.Id);
        foreach (var productReview in productReviews)
        {
            var product = await _productService.GetProductById(productReview.ProductId);

            var reviewModel = new CustomerProductReviewModel {
                Id = productReview.Id,
                ProductId = productReview.ProductId,
                ProductName = product.Name,
                ProductSeName = product.GetSeName(request.Language.Id),
                Rating = productReview.Rating,
                CreatedOn = _dateTimeService.ConvertToUserTime(productReview.CreatedOnUtc, DateTimeKind.Utc),
                Signature = productReview.Signature,
                ReviewText = productReview.ReviewText,
                ReplyText = productReview.ReplyText,
                IsApproved = productReview.IsApproved
            };

            reviewsModel.Reviews.Add(reviewModel);
        }

        return reviewsModel;
    }
}