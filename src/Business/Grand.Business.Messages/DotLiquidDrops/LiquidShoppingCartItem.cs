using DotLiquid;
using Grand.Business.Common.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using System.Collections.Generic;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops
{
    public partial class LiquidShoppingCartItem : Drop
    {
        private Product _product;
        private Language _language;
        private string _attributeDescription;
        private string _pictureUrl;
        private ShoppingCartItem _shoppingCartItem;

        public LiquidShoppingCartItem(Product product,
            string attributeDescription,
            string pictureUrl,
            ShoppingCartItem shoppingCartItem,
            Language language)
        {
            _language = language;
            _product = product;
            _attributeDescription = attributeDescription;
            _pictureUrl = pictureUrl;
            _shoppingCartItem = shoppingCartItem;
            AdditionalTokens = new Dictionary<string, string>();
        }

        public string AttributeDescription
        {
            get
            {
                return _attributeDescription;
            }
        }

        public string PictureUrl
        {
            get
            {
                return _pictureUrl;
            }
        }

        public int Quantity
        {
            get
            {
                return _shoppingCartItem.Quantity;
            }
        }
        public ShoppingCartType ShoppingCartType
        {
            get
            {
                return _shoppingCartItem.ShoppingCartTypeId;
            }
        }

        public string ProductName
        {
            get
            {
                string name = "";

                if (_product != null)
                    name = WebUtility.HtmlEncode(_product.GetTranslation(x => x.Name, _language.Id));

                return name;
            }
        }

        public string ProductSeName
        {
            get
            {
                string name = "";

                if (_product != null)
                    name = _product.GetTranslation(x => x.SeName, _language.Id);
                return name;
            }
        }
        public string ProductShortDescription
        {
            get
            {
                string desc = "";

                if (_product != null)
                    desc = WebUtility.HtmlEncode(_product.GetTranslation(x => x.ShortDescription, _language.Id));

                return desc;
            }
        }

        public string ProductFullDescription
        {
            get
            {
                string desc = "";

                if (_product != null)
                    desc = WebUtility.HtmlDecode(_product.GetTranslation(x => x.FullDescription, _language.Id));

                return desc;
            }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}