using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Common
{
    public partial class StoreModel : BaseEntityModel
    {
        public string Name { get; set; }
        public string Shortcut { get; set; }
    }
}