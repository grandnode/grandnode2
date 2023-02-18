using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.ShoppingCart
{
    public class WishlistEmailAFriendModel : BaseModel
    {
        public WishlistEmailAFriendModel()
        {
            Captcha = new CaptchaModel();
        }

        [GrandResourceDisplayName("Wishlist.EmailAFriend.FriendEmail")]
        public string FriendEmail { get; set; }

        [GrandResourceDisplayName("Wishlist.EmailAFriend.YourEmailAddress")]
        public string YourEmailAddress { get; set; }

        [GrandResourceDisplayName("Wishlist.EmailAFriend.PersonalMessage")]
        public string PersonalMessage { get; set; }

        public bool SuccessfullySent { get; set; }
        public string Result { get; set; }
        public bool DisplayCaptcha { get; set; }
        public ICaptchaValidModel Captcha { get; set; }
    }
}