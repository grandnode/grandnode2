using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetReviewsHandler : IRequestHandler<GetReviews, CustomerProductReviewsModel>
    {
        private readonly ITranslationService _translationService;
        private readonly IProductService _productService;
        private readonly IProductReviewService _productReviewService;
        private readonly IDateTimeService _dateTimeService;

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
            var reviewsModel = new CustomerProductReviewsModel();

            reviewsModel.CustomerId = request.Customer.Id;
            reviewsModel.CustomerInfo = request.Customer != null ? !string.IsNullOrEmpty(request.Customer.Email) ? request.Customer.Email : _translationService.GetResource("Admin.Customers.Guest") : "";

            var productReviews = await _productReviewService.GetAllProductReviews(request.Customer.Id);
            foreach (var productReview in productReviews)
            {
                var product = await _productService.GetProductById(productReview.ProductId);

                var reviewModel = new CustomerProductReviewModel();

                reviewModel.Id = productReview.Id;
                reviewModel.ProductId = productReview.ProductId;
                reviewModel.ProductName = product.Name;
                reviewModel.ProductSeName = product.GetSeName(request.Language.Id);
                reviewModel.Rating = productReview.Rating;
                reviewModel.CreatedOn = _dateTimeService.ConvertToUserTime(productReview.CreatedOnUtc, DateTimeKind.Utc);
                reviewModel.Signature = productReview.Signature;
                reviewModel.ReviewText = productReview.ReviewText;
                reviewModel.ReplyText = productReview.ReplyText;
                reviewModel.IsApproved = productReview.IsApproved;

                reviewsModel.Reviews.Add(reviewModel);
            }

            return reviewsModel;
        }
    }
}
