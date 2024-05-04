using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Web.Commands.Models.Products;
using MediatR;

namespace Grand.Web.Commands.Handler.Products;

public class InsertProductReviewCommandHandler : IRequestHandler<InsertProductReviewCommand, ProductReview>
{
    private readonly CatalogSettings _catalogSettings;
    private readonly ICustomerService _customerService;
    private readonly LanguageSettings _languageSettings;
    private readonly IMediator _mediator;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IProductReviewService _productReviewService;

    public InsertProductReviewCommandHandler(
        IProductReviewService productReviewService,
        ICustomerService customerService,
        IMessageProviderService messageProviderService,
        IMediator mediator,
        CatalogSettings catalogSettings,
        LanguageSettings languageSettings)
    {
        _productReviewService = productReviewService;
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _mediator = mediator;
        _catalogSettings = catalogSettings;
        _languageSettings = languageSettings;
    }

    public async Task<ProductReview> Handle(InsertProductReviewCommand request, CancellationToken cancellationToken)
    {
        //save review
        var rating = request.Model.AddProductReview.Rating;
        if (rating is < 1 or > 5)
            rating = _catalogSettings.DefaultProductRatingValue;
        var isApproved = !_catalogSettings.ProductReviewsMustBeApproved;

        var confirmPurchased =
            (await _mediator.Send(new GetOrderQuery {
                CustomerId = request.Customer.Id,
                StoreId = request.Store.Id,
                ProductId = request.Product.Id,
                Os = (int)OrderStatusSystem.Complete,
                PageSize = 1
            }, cancellationToken)).Any();

        var productReview = new ProductReview {
            ProductId = request.Product.Id,
            StoreId = request.Store.Id,
            CustomerId = request.Customer.Id,
            Title = request.Model.AddProductReview.Title,
            ReviewText = request.Model.AddProductReview.ReviewText,
            Rating = rating,
            HelpfulYesTotal = 0,
            HelpfulNoTotal = 0,
            IsApproved = isApproved,
            ConfirmedPurchase = confirmPurchased
        };

        await _productReviewService.InsertProductReview(productReview);

        if (!request.Customer.HasContributions) await _customerService.UpdateContributions(request.Customer);

        //update product totals
        await _mediator.Send(new UpdateProductReviewTotalsCommand { Product = request.Product }, cancellationToken);

        //notify store owner
        if (_catalogSettings.NotifyStoreOwnerAboutNewProductReviews)
            await _messageProviderService.SendProductReviewMessage(request.Product, productReview, request.Store,
                _languageSettings.DefaultAdminLanguageId);

        return productReview;
    }
}