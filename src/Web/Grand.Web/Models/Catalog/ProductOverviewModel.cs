﻿using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Tax;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.Catalog
{
    public class ProductOverviewModel : BaseEntityModel
    {
        public ProductOverviewModel()
        {
            ProductPrice = new ProductPriceModel();
            DefaultPictureModel = new PictureModel();
            SecondPictureModel = new PictureModel();
            SpecificationAttributeModels = new List<ProductSpecificationModel>();
            ProductAttributeModels = new List<ProductAttributeModel>();
            ReviewOverviewModel = new ProductReviewOverviewModel();
        }
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

        //price
        public ProductPriceModel ProductPrice { get; set; }

        //picture
        public PictureModel DefaultPictureModel { get; set; }
        public PictureModel SecondPictureModel { get; set; }

        //specification attributes
        public IList<ProductSpecificationModel> SpecificationAttributeModels { get; set; }

        //product attributes 
        public IList<ProductAttributeModel> ProductAttributeModels { get; set; }

        //price
        public ProductReviewOverviewModel ReviewOverviewModel { get; set; }

        #region Nested Classes
        public class ProductPriceModel : BaseModel
        {
            public ProductPriceModel()
            {
                AppliedDiscounts = new List<ApplyDiscount>();
            }

            public string OldPrice { get; set; }
            public double OldPriceValue { get; set; }
            public string CatalogPrice { get; set; }
            public string Price { get; set; }
            public double PriceValue { get; set; }
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

            public List<ApplyDiscount> AppliedDiscounts { get; set; }
            public TierPrice PreferredTierPrice { get; set; }

        }

        public class ProductAttributeModel : BaseModel
        {
            public ProductAttributeModel()
            {
                Values = new List<ProductAttributeValueModel>();
            }
            public string Name { get; set; }
            public string SeName { get; set; }
            public string TextPrompt { get; set; }
            public bool IsRequired { get; set; }
            public AttributeControlType AttributeControlType { get; set; }
            public IList<ProductAttributeValueModel> Values { get; set; }
        }

        public class ProductAttributeValueModel : BaseModel
        {
            public ProductAttributeValueModel()
            {
                ImageSquaresPictureModel = new PictureModel();
                PictureModel = new PictureModel();
            }
            public string Name { get; set; }
            public string ColorSquaresRgb { get; set; }
            //picture model is used with "image square" attribute type
            public PictureModel ImageSquaresPictureModel { get; set; }
            public PictureModel PictureModel { get; set; }
        }
        #endregion
    }
}