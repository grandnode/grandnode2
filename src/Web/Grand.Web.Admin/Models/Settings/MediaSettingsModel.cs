using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Settings
{
    public partial class MediaSettingsModel : BaseModel
    {
        #region Standard Media Settings
        public string ActiveStore { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.PicturesStoredIntoDatabase")]
        public bool PicturesStoredIntoDatabase { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.ProductThumbPictureSize")]
        public int ProductThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.ProductDetailsPictureSize")]
        public int ProductDetailsPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.ProductThumbPictureSizeOnProductDetailsPage")]
        public int ProductThumbPictureSizeOnProductDetailsPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.AssociatedProductPictureSize")]
        public int AssociatedProductPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.CategoryThumbPictureSize")]
        public int CategoryThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.BrandThumbPictureSize")]
        public int BrandThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.CollectionThumbPictureSize")]
        public int CollectionThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.VendorThumbPictureSize")]
        public int VendorThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.CartThumbPictureSize")]
        public int CartThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.MiniCartThumbPictureSize")]
        public int MiniCartThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.MaximumImageSize")]
        public int MaximumImageSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.DefaultPictureZoomEnabled")]
        public bool DefaultPictureZoomEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.AddToCartThumbPictureSize")]
        public int AddToCartThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.AutoCompleteSearchThumbPictureSize")]
        public int AutoCompleteSearchThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.BlogThumbPictureSize")]
        public int BlogThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.CourseThumbPictureSize")]
        public int CourseThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.ImageSquarePictureSize")]
        public int ImageSquarePictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.LessonThumbPictureSize")]
        public int LessonThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.NewsListThumbPictureSize")]
        public int NewsListThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.NewsThumbPictureSize")]
        public int NewsThumbPictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.ProductBundlePictureSize")]
        public int ProductBundlePictureSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.AllowedFileTypes")]
        public string AllowedFileTypes { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Media.DefaultImageName")]
        public string DefaultImageName { get; set; } = "no-image.png";

        #endregion
    }
}