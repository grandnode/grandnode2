using Grand.Infrastructure.Models;

namespace Authentication.Google.Models
{
    public class FailedModel : BaseModel
    {
        public string ErrorMessage { get; set; }
    }
}
