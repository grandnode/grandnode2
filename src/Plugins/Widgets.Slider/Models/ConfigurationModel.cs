using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Widgets.Slider.Models
{
    public class ConfigurationModel : BaseModel
    {
        
        [GrandResourceDisplayName("Widgets.Slider.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Widgets.Slider.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }

        //Store acl
        [GrandResourceDisplayName("Widgets.Slider.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }
    }
}