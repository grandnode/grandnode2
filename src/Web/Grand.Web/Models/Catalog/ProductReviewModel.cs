using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog
{
    public class ProductReviewOverviewModel : BaseModel
    {
        public string ProductId { get; set; }

        public int RatingSum { get; set; }

        public int TotalReviews { get; set; }
        public double AvgRating { get; set; }

        public bool AllowCustomerReviews { get; set; }
    }

    public class ProductReviewsModel : BaseModel
    {
        public ProductReviewsModel()
        {
            Items = new List<ProductReviewModel>();
            AddProductReview = new AddProductReviewModel();
            Captcha = new CaptchaModel();
        }
        public string ProductId { get; set; }

        public bool CaptchaValid { get; set; }
        public ICaptchaValidModel Captcha { get; set; }
        public string ProductName { get; set; }

        public string ProductSeName { get; set; }

        public IList<ProductReviewModel> Items { get; set; }
        public AddProductReviewModel AddProductReview { get; set; }
        public ProductReviewOverviewModel ProductReviewOverviewModel { get; set; }
    }

    public class ProductReviewModel : BaseEntityModel
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Title { get; set; }
        public string ReviewText { get; set; }
        public string ReplyText { get; set; }
        public string Signature { get; set; }
        public bool ConfirmedPurchase { get; set; }
        public int Rating { get; set; }
        public ProductReviewHelpfulnessModel Helpfulness { get; set; }
        public string WrittenOnStr { get; set; }
    }


    public class ProductReviewHelpfulnessModel : BaseModel
    {
        public string ProductReviewId { get; set; }
        public string ProductId { get; set; }

        public int HelpfulYesTotal { get; set; }

        public int HelpfulNoTotal { get; set; }
    }

    public class AddProductReviewModel : BaseModel
    {
        [GrandResourceDisplayName("Reviews.Fields.Title")]
        public string Title { get; set; }

        [GrandResourceDisplayName("Reviews.Fields.ReviewText")]
        public string ReviewText { get; set; }

        [GrandResourceDisplayName("Reviews.Fields.Rating")]
        public int Rating { get; set; }

        public bool DisplayCaptcha { get; set; }

        public bool CanCurrentCustomerLeaveReview { get; set; }
        public bool SuccessfullyAdded { get; set; }
        public string Result { get; set; }
    }
}