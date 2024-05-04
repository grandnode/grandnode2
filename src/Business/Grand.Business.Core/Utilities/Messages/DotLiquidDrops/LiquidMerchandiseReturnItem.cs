using DotLiquid;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using System.Net;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidMerchandiseReturnItem : Drop
{
    private readonly MerchandiseReturnItem _item;
    private readonly string _languageId;
    private readonly OrderItem _orderItem;
    private readonly Product _product;

    public LiquidMerchandiseReturnItem(MerchandiseReturnItem item, OrderItem orderItem, Product product,
        string languageid)
    {
        _item = item;
        _orderItem = orderItem;
        _languageId = languageid;
        _product = product;
        AdditionalTokens = new Dictionary<string, string>();
    }


    public string ProductName {
        get {
            var name = "";

            if (_product != null)
                name = WebUtility.HtmlEncode(_product.Name);

            return name;
        }
    }

    public string ProductSeName {
        get {
            var name = "";

            if (_product != null)
                name = _product.SeName;
            return name;
        }
    }

    public string ProductShortDescription {
        get {
            var desc = "";

            if (_product != null)
                desc = WebUtility.HtmlEncode(_product.ShortDescription);

            return desc;
        }
    }

    public string ProductFullDescription {
        get {
            var desc = "";

            if (_product != null)
                desc = WebUtility.HtmlDecode(_product.FullDescription);

            return desc;
        }
    }


    public string ProductId => _product.Id;

    public int Quantity => _item.Quantity;

    public string ReasonForReturn => _item.ReasonForReturn;

    public string RequestedAction => _item.RequestedAction;

    public double UnitPriceWithoutDiscInclTax => _orderItem.UnitPriceWithoutDiscInclTax;

    public double UnitPriceWithoutDiscExclTax => _orderItem.UnitPriceWithoutDiscExclTax;

    public double UnitPriceInclTax => _orderItem.UnitPriceInclTax;

    public double UnitPriceExclTax => _orderItem.UnitPriceExclTax;

    public double PriceInclTax => _orderItem.PriceInclTax;

    public double PriceExclTax => _orderItem.PriceExclTax;

    public double DiscountAmountInclTax => _orderItem.DiscountAmountInclTax;

    public double DiscountAmountExclTax => _orderItem.DiscountAmountExclTax;

    public double OriginalProductCost => _orderItem.OriginalProductCost;

    public string AttributeDescription => _orderItem.AttributeDescription;


    public IDictionary<string, string> AdditionalTokens { get; set; }
}