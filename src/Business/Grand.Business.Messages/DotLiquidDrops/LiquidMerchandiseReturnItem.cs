using DotLiquid;
using Grand.Business.Common.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using System.Collections.Generic;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops
{
    public partial class LiquidMerchandiseReturnItem : Drop
    {
        private MerchandiseReturnItem _item;
        private Product _product;
        private OrderItem _orderItem;
        private string _languageId;

        public LiquidMerchandiseReturnItem(MerchandiseReturnItem item, OrderItem orderItem, Product product, string languageid)
        {
            _item = item;
            _orderItem = orderItem;
            _languageId = languageid;
            _product = product;
            AdditionalTokens = new Dictionary<string, string>();
        }


        public string ProductName
        {
            get
            {
                string name = "";

                if (_product != null)
                    name = WebUtility.HtmlEncode(_product.GetTranslation(x => x.Name, _languageId));

                return name;
            }
        }

        public string ProductSeName
        {
            get
            {
                string name = "";

                if (_product != null)
                    name = _product.GetTranslation(x => x.SeName, _languageId);
                return name;
            }
        }
        public string ProductShortDescription
        {
            get
            {
                string desc = "";

                if (_product != null)
                    desc = WebUtility.HtmlEncode(_product.GetTranslation(x => x.ShortDescription, _languageId));

                return desc;
            }
        }

        public string ProductFullDescription
        {
            get
            {
                string desc = "";

                if (_product != null)
                    desc = WebUtility.HtmlDecode(_product.GetTranslation(x => x.FullDescription, _languageId));

                return desc;
            }
        }


        public string ProductId
        {
            get
            {
                return _product.Id;
            }
        }

        public int Quantity
        {
            get
            {
                return _item.Quantity;
            }
        }

        public string ReasonForReturn
        {
            get
            {
                return _item.ReasonForReturn;
            }
        }

        public string RequestedAction
        {
            get
            {
                return _item.RequestedAction;
            }
        }
        public double UnitPriceWithoutDiscInclTax
        {
            get
            {
                return _orderItem.UnitPriceWithoutDiscInclTax;
            }
        }

        public double UnitPriceWithoutDiscExclTax
        {
            get
            {
                return _orderItem.UnitPriceWithoutDiscExclTax;
            }
        }

        public double UnitPriceInclTax
        {
            get
            {
                return _orderItem.UnitPriceInclTax;
            }
        }

        public double UnitPriceExclTax
        {
            get
            {
                return _orderItem.UnitPriceExclTax;
            }
        }

        public double PriceInclTax
        {
            get
            {
                return _orderItem.PriceInclTax;
            }
        }

        public double PriceExclTax
        {
            get
            {
                return _orderItem.PriceExclTax;
            }
        }

        public double DiscountAmountInclTax
        {
            get
            {
                return _orderItem.DiscountAmountInclTax;
            }
        }

        public double DiscountAmountExclTax
        {
            get
            {
                return _orderItem.DiscountAmountExclTax;
            }
        }

        public double OriginalProductCost
        {
            get
            {
                return _orderItem.OriginalProductCost;
            }
        }

        public string AttributeDescription
        {
            get
            {
                return _orderItem.AttributeDescription;
            }
        }


        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}