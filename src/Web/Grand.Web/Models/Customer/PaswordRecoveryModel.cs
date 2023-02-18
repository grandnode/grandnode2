﻿using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Customer
{
    public class PasswordRecoveryModel : BaseModel
    {
        public PasswordRecoveryModel()
        {
            Captcha = new CaptchaModel();
        }
        [DataType(DataType.EmailAddress)]
        [GrandResourceDisplayName("Account.PasswordRecovery.Email")]
        public string Email { get; set; }
        public string Result { get; set; }
        public bool Send { get; set; }
        public bool DisplayCaptcha { get; set; }
        public ICaptchaValidModel Captcha { get; set; }
    }
}