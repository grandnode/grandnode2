using System.Collections.Generic;

namespace Grand.Web.Common.Security.Captcha
{
    public class GoogleReCaptchaResponse
    {
        public bool IsValid { get; set; }
        public List<string> ErrorCodes { get; set; }

        public GoogleReCaptchaResponse()
        {
            ErrorCodes = new List<string>();
        }
    }
}