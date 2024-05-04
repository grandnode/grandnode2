using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Widgets.Slider.Models;

public class SlideModel : BaseEntityModel, ILocalizedModel<SlideLocalizedModel>, IStoreLinkModel
{
    [GrandResourceDisplayName("Widgets.Slider.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.Description")]
    public string Description { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.Link")]
    public string Link { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.Published")]
    public bool Published { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.FullWidth")]
    public bool FullWidth { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.SliderType")]
    public int SliderTypeId { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.Picture")]
    [UIHint("Picture")]
    public string PictureId { get; set; }

    [UIHint("Category")]
    [GrandResourceDisplayName("Widgets.Slider.Category")]
    public string CategoryId { get; set; }

    [UIHint("Collection")]
    [GrandResourceDisplayName("Widgets.Slider.Collection")]
    public string CollectionId { get; set; }

    [UIHint("Brand")]
    [GrandResourceDisplayName("Widgets.Slider.Brand")]
    public string BrandId { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.StartDate")]
    [UIHint("DateTimeNullable")]
    public DateTime? StartDateUtc { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.EndDate")]
    [UIHint("DateTimeNullable")]
    public DateTime? EndDateUtc { get; set; }

    public IList<SlideLocalizedModel> Locales { get; set; } = new List<SlideLocalizedModel>();

    //Store acl
    [GrandResourceDisplayName("Widgets.Slider.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }
}

public class SlideLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Widgets.Slider.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Widgets.Slider.Description")]

    public string Description { get; set; }

    public string LanguageId { get; set; }
}