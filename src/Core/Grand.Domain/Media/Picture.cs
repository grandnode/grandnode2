using Grand.Domain.Common;
using Grand.Domain.Localization;

namespace Grand.Domain.Media
{
    /// <summary>
    /// Represents a picture
    /// </summary>
    public partial class Picture : BaseEntity, ITranslationEntity
    {
        public Picture()
        {
            Locales = new List<TranslationEntity>();
        }

        /// <summary>
        /// Gets or sets the picture binary
        /// </summary>
        public byte[] PictureBinary { get; set; }

        /// <summary>
        /// Gets or sets an reference identifier
        /// </summary>
        public Reference Reference { get; set; }

        /// <summary>
        /// Gets or sets an object reference identifier
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the picture mime type
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the SEO friednly filename of the picture
        /// </summary>
        public string SeoFilename { get; set; }

        /// <summary>
        /// Gets or sets the "alt" attribute for "img" HTML element. If empty, then a default rule will be used (e.g. product name)
        /// </summary>
        public string AltAttribute { get; set; }

        /// <summary>
        /// Gets or sets the "title" attribute for "img" HTML element. If empty, then a default rule will be used (e.g. product name)
        /// </summary>
        public string TitleAttribute { get; set; }

        /// <summary>
        /// Gets or sets the "Style" attribute for "img" HTML element. 
        /// </summary>
        public string Style { get; set; }

        /// <summary>
        /// Gets or sets the "extra field"
        /// </summary>
        public string ExtraField { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the picture is new
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }

    }
}
