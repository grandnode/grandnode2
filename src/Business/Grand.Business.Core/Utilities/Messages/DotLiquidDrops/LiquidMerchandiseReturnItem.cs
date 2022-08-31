using DotLiquid;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using System.Net;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidMerchandiseReturnItem : Drop
    {
        private readonly MerchandiseReturnItem _item;
        private readonly Product _product;
        private readonly OrderItem _orderItem;
        private readonly string _languageId;

        public LiquidMerchandiseReturnItem(MerchandiseReturnItem item, OrderItem orderItem, Product product, string languageid)
        {
            _item = item;
            _orderItem = orderItem;
            _languageId = languageid;
            _product = product;
            AdditionalTokens = new Dictionary<string, string>();
        }


        public string ProductName {
            get {
                string name = "";

                if (_product != null)
                    name = WebUtility.HtmlEncode(_product.Name);

                return name;
            }
        }

        public string ProductSeName {
            get {
                string name = "";

                if (_product != null)
                    name = _product.SeName;
                return name;
            }
        }
        public string ProductShortDescription {
            get {
                string desc = "";

                if (_product != null)
                    desc = WebUtility.HtmlEncode(_product.ShortDescription);

                return desc;
            }
        }

        public string ProductFullDescription {
            get {
                string desc = "";

                if (_product != null)
                    desc = WebUtility.HtmlDecode(_product.FullDescription);

                return desc;
            }
        }


        public string ProductId {
            get {
                return _product.Id;
            }
        }

        public int Quantity {
            get {
                return _item.Quantity;
            }
        }

        public string ReasonForReturn {
            get {
                return _item.ReasonForReturn;
            }
        }

        public string RequestedAction {
            get {
                return _item.RequestedAction;
            }
        }
        public double UnitPriceWithoutDiscInclTax {
            get {
                return _orderItem.UnitPriceWithoutDiscInclTax;
            }
        }

        public double UnitPriceWithoutDiscExclTax {
            get {
                return _orderItem.UnitPriceWithoutDiscExclTax;
            }
        }

        public double UnitPriceInclTax {
            get {
                return _orderItem.UnitPriceInclTax;
            }
        }

        public double UnitPriceExclTax {
            get {
                return _orderItem.UnitPriceExclTax;
            }
        }

        public double PriceInclTax {
            get {
                return _orderItem.PriceInclTax;
            }
        }

        public double PriceExclTax {
            get {
                return _orderItem.PriceExclTax;
            }
        }

        public double DiscountAmountInclTax {
            get {
                return _orderItem.DiscountAmountInclTax;
            }
        }

        public double DiscountAmountExclTax {
            get {
                return _orderItem.DiscountAmountExclTax;
            }
        }

        public double OriginalProductCost {
            get {
                return _orderItem.OriginalProductCost;
            }
        }

        public string AttributeDescription {
            get {
                return _orderItem.AttributeDescription;
            }
        }


        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}