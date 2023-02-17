using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Vendors
{
    public class ApplyVendorModel : BaseModel
    {

        public ApplyVendorModel()
        {
            Address = new VendorAddressModel();
            Captcha = new CaptchaModel();
        }

        public VendorAddressModel Address { get; set; }

        [GrandResourceDisplayName("Vendors.ApplyAccount.Name")]
        public string Name { get; set; }
        [DataType(DataType.EmailAddress)]
        [GrandResourceDisplayName("Vendors.ApplyAccount.Email")]
        public string Email { get; set; }
        [GrandResourceDisplayName("Vendors.ApplyAccount.Description")]
        public string Description { get; set; }
        public bool DisplayCaptcha { get; set; }
        public ICaptchaValidModel Captcha { get; set; }
        public bool TermsOfServiceEnabled { get; set; }
        public bool TermsOfServicePopup { get; set; }
        public bool DisableFormInput { get; set; }
        public bool AllowToUploadFile { get; set; }
        public string Result { get; set; }
    }
}