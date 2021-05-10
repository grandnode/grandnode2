using Grand.Domain.Configuration;

namespace Grand.Domain.Media
{
    public class MediaSettings : ISettings
    {
        public string DefaultImageName { get; set; } = "no-image.png";
        public int BlogThumbPictureSize { get; set; }
        public int NewsThumbPictureSize { get; set; }
        public int NewsListThumbPictureSize { get; set; }
        public int ProductThumbPictureSize { get; set; }
        public int ProductDetailsPictureSize { get; set; }
        public int ProductBundlePictureSize { get; set; }
        public int ProductThumbPictureSizeOnProductDetailsPage { get; set; }
        public int AssociatedProductPictureSize { get; set; }
        public int CategoryThumbPictureSize { get; set; }
        public int BrandThumbPictureSize { get; set; }
        public int CollectionThumbPictureSize { get; set; }
        public int VendorThumbPictureSize { get; set; }
        public int CourseThumbPictureSize { get; set; }
        public int LessonThumbPictureSize { get; set; }
        public int CartThumbPictureSize { get; set; }
        public int MiniCartThumbPictureSize { get; set; }
        public int AddToCartThumbPictureSize { get; set; }
        public int AutoCompleteSearchThumbPictureSize { get; set; }
        public int ImageSquarePictureSize { get; set; }
        public bool DefaultPictureZoomEnabled { get; set; }

        public int MaximumImageSize { get; set; }

        public string AllowedFileTypes { get; set; }
        public string StoreLocation { get; set; }

        /// <summary>
        /// Gets a value indicating whether the images should be stored in data base.
        /// </summary>
        public bool StoreInDb { get; set; } = true;
    }
}