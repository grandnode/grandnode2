﻿using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Vendors
{
    public class VendorReviewOverviewModel : BaseModel
    {
        public string VendorId { get; set; }

        public int RatingSum { get; set; }

        public int TotalReviews { get; set; }

        public bool AllowCustomerReviews { get; set; }
    }

    public class VendorReviewsModel : BaseModel
    {
        public VendorReviewsModel()
        {
            Items = new List<VendorReviewModel>();
            AddVendorReview = new AddVendorReviewModel();
            Captcha = new CaptchaModel();
        }
        public string VendorId { get; set; }

        public string VendorName { get; set; }

        public string VendorSeName { get; set; }

        public IList<VendorReviewModel> Items { get; set; }
        public AddVendorReviewModel AddVendorReview { get; set; }
        public VendorReviewOverviewModel VendorReviewOverview { get; set; }
        public ICaptchaValidModel Captcha { get; set; }
    }

    public class VendorReviewModel : BaseEntityModel
    {
        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string Title { get; set; }

        public string ReviewText { get; set; }

        public int Rating { get; set; }

        public VendorReviewHelpfulnessModel Helpfulness { get; set; }

        public string WrittenOnStr { get; set; }
    }

    public class VendorReviewHelpfulnessModel : BaseModel
    {
        public string VendorReviewId { get; set; }
        public string VendorId { get; set; }

        public int HelpfulYesTotal { get; set; }

        public int HelpfulNoTotal { get; set; }
    }

    public class AddVendorReviewModel : BaseModel
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
        public bool NotAllowAnonymousUsersToReviewVendor { get; set; }
    }
}