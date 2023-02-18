﻿using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Vendors
{
    public class ContactVendorModel : BaseModel
    {
        public ContactVendorModel()
        {
            Captcha = new CaptchaModel();
        }
        public string VendorId { get; set; }
        public string VendorName { get; set; }

        [DataType(DataType.EmailAddress)]
        [GrandResourceDisplayName("ContactVendor.Email")]
        public string Email { get; set; }

        [GrandResourceDisplayName("ContactVendor.Subject")]
        public string Subject { get; set; }
        public bool SubjectEnabled { get; set; }

        [GrandResourceDisplayName("ContactVendor.Enquiry")]
        public string Enquiry { get; set; }

        [GrandResourceDisplayName("ContactVendor.FullName")]
        public string FullName { get; set; }

        public bool SuccessfullySent { get; set; }
        public string Result { get; set; }

        public bool DisplayCaptcha { get; set; }
        public ICaptchaValidModel Captcha { get; set; }
    }
}