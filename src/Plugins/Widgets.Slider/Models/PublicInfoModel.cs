using Grand.Infrastructure.Models;

namespace Widgets.Slider.Models;

public class PublicInfoModel : BaseModel
{
    public IList<Slider> Slide { get; set; } = new List<Slider>();

    public class Slider
    {
        public string PictureUrl { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public bool FullWidth { get; set; }
        public string CssClass { get; set; }
    }
}