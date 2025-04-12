using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.PushNotifications;

public class ReceiversModel : BaseModel
{
    public int Allowed { get; set; }

    public int Denied { get; set; }
}