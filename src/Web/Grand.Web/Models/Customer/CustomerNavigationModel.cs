using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Customer
{
    public partial class CustomerNavigationModel : BaseModel
    {
        public bool HideInfo { get; set; }
        public bool HideAddresses { get; set; }
        public bool HideOrders { get; set; }
        public bool HideOutOfStockSubscriptions { get; set; }
        public bool HideMerchandiseReturns { get; set; }
        public bool HideDownloadableProducts { get; set; }
        public bool HideLoyaltyPoints { get; set; }
        public bool HideChangePassword { get; set; }
        public bool HideDeleteAccount { get; set; }
        public bool HideAuctions { get; set; }
        public bool HideNotes { get; set; }
        public bool HideDocuments { get; set; }
        public bool ShowVendorInfo { get; set; }
        public bool HideReviews { get; set; }
        public bool HideCourses { get; set; }
        public bool HideSubAccounts { get; set; }
        public AccountNavigationEnum SelectedTab { get; set; }
    }

    public enum AccountNavigationEnum
    {
        Info = 0,
        Addresses = 10,
        Orders = 20,
        OutOfStockSubscriptions = 30,
        MerchandiseReturns = 40,
        DownloadableProducts = 50,
        LoyaltyPoints = 60,
        ChangePassword = 70,
        DeleteAccount = 75,
        VendorInfo = 100,
        Auctions = 110,
        Notes = 120,
        Documents = 130,
        Reviews = 140,
        Courses = 150,
        SubAccounts = 160,
        Others = 900
    }
}