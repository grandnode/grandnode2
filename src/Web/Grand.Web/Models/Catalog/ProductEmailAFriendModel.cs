﻿using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog
{
    public class ProductEmailAFriendModel : BaseModel
    {
        public ProductEmailAFriendModel()
        {
            Captcha = new CaptchaModel();
        }
        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductSeName { get; set; }

        [GrandResourceDisplayName("Products.EmailAFriend.FriendEmail")]
        public string FriendEmail { get; set; }

        [GrandResourceDisplayName("Products.EmailAFriend.YourEmailAddress")]
        public string YourEmailAddress { get; set; }

        [GrandResourceDisplayName("Products.EmailAFriend.PersonalMessage")]
        public string PersonalMessage { get; set; }

        public bool SuccessfullySent { get; set; }
        public string Result { get; set; }
        public bool DisplayCaptcha { get; set; }
        public ICaptchaValidModel Captcha { get; set; }
    }
}