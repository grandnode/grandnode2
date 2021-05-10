using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Cms
{
    public partial class WidgetModel : BaseModel
    {
        public string WidgetZone { get; set; }
        public string ViewComponentName { get; set; }
        public object AdditionalData { get; set; }
    }
}