using Grand.Infrastructure.Models;

namespace Authentication.Facebook.Models
{
    public class FailedModel : BaseModel
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
