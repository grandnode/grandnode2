using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Tax;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.Catalog;

public class ProductOverviewModel : BaseEntityModel
{
    public string Name { get; set; }
    public string ShortDescription { get; set; }
    public string FullDescription { get; set; }
    public string SeName { get; set; }
    public string Url { get; set; }
    public ProductType ProductType { get; set; }
    public bool MarkAsNew { get; set; }
    public string Sku { get; set; }
    public string Flag { get; set; }
    public string Gtin { get; set; }
    public string Mpn { get; set; }
    public string BrandName { get; set; }
    public bool IsFreeShipping { get; set; }
    public bool ShowSku { get; set; }
    public bool ShowQty { get; set; }
    public bool LowStock { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? EndTimeLocalTime { get; set; }
    public TaxDisplayType TaxDisplayType { get; set; }

    public int Interval { get; set; }

    public IntervalUnit IntervalUnitId { get; set; }

    //price
    public ProductPriceModel ProductPrice { get; set; } = new();

    //picture
    public PictureModel DefaultPictureModel { get; set; } = new();
    public PictureModel SecondPictureModel { get; set; } = new();

    //specification attributes
    public IList<ProductSpecificationModel> SpecificationAttributeModels { get; set; } =
        new List<ProductSpecificationModel>();

    //product attributes 
    public IList<ProductAttributeModel> ProductAttributeModels { get; set; } = new List<ProductAttributeModel>();

    //price
    public ProductReviewOverviewModel ReviewOverviewModel { get; set; } = new();

    #region Nested Classes

    public class ProductPriceModel : BaseModel
    {
        public string OldPrice { get; set; }
        public double OldPriceValue { get; set; }
        public string CatalogPrice { get; set; }
        public string Price { get; set; }
        public bool CallForPrice { get; set; }
        public double PriceValue { get; set; }
        public bool PriceIncludesTax { get; set; }
        public string StartPrice { get; set; }
        public double StartPriceValue { get; set; }
        public string HighestBid { get; set; }
        public double HighestBidValue { get; set; }
        public string BasePricePAngV { get; set; }
        public bool DisableBuyButton { get; set; }
        public bool DisableWishlistButton { get; set; }
        public bool DisableAddToCompareListButton { get; set; }
        public bool AvailableForPreOrder { get; set; }
        public DateTime? PreOrderDateTimeUtc { get; set; }
        public bool ForceRedirectionAfterAddingToCart { get; set; }

        public List<ApplyDiscount> AppliedDiscounts { get; set; } = new();
        public TierPrice PreferredTierPrice { get; set; }
    }

    public class ProductAttributeModel : BaseModel
    {
        public string Name { get; set; }
        public string SeName { get; set; }
        public string TextPrompt { get; set; }
        public bool IsRequired { get; set; }
        public AttributeControlType AttributeControlType { get; set; }
        public IList<ProductAttributeValueModel> Values { get; set; } = new List<ProductAttributeValueModel>();
    }

    public class ProductAttributeValueModel : BaseModel
    {
        public string Name { get; set; }

        public string ColorSquaresRgb { get; set; }

        //picture model is used with "image square" attribute type
        public PictureModel ImageSquaresPictureModel { get; set; } = new();
        public PictureModel PictureModel { get; set; } = new();
    }

    #endregion
}