using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Events.Catalog
{
    /// <summary>
    /// Event for product review  approval
    /// </summary>
    public class ProductReviewApprovedEvent : INotification
    {
        public ProductReviewApprovedEvent(ProductReview productReview)
        {
            ProductReview = productReview;
        }

        public ProductReview ProductReview { get; private set; }
    }
}