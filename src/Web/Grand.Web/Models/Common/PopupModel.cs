using Grand.Domain.Messages;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Common
{
    public class PopupModel : BaseEntityModel
    {
        public string Name { get; set; }
        public string Body { get; set; }
        public string CustomerActionId { get; set; }
        public PopupType PopupType { get; set; }
    }
}
