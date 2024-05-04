using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Common;

public class PictureModel : BaseEntityModel, ILocalizedModel<PictureModel.PictureLocalizedModel>
{
    public string ObjectId { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Picture")]
    public string PictureUrl { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Picture.Fields.AltAttribute")]
    public string AltAttribute { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Picture.Fields.TitleAttribute")]
    public string TitleAttribute { get; set; }


    [GrandResourceDisplayName("Admin.Catalog.Picture.Fields.Style")]
    public string Style { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Picture.Fields.ExtraField")]
    public string ExtraField { get; set; }

    public IList<PictureLocalizedModel> Locales { get; set; } = new List<PictureLocalizedModel>();

    public class PictureLocalizedModel : ILocalizedModelLocal
    {
        [GrandResourceDisplayName("Admin.Catalog.Picture.Fields.AltAttribute")]
        public string AltAttribute { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Picture.Fields.TitleAttribute")]
        public string TitleAttribute { get; set; }

        public string LanguageId { get; set; }
    }
}