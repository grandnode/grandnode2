using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Catalog.Events.Models
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