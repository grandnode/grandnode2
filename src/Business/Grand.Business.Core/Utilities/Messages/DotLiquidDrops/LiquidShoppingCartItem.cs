using DotLiquid;
using Grand.Business.Core.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using System.Net;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidShoppingCartItem : Drop
{
    private readonly Language _language;
    private readonly Product _product;
    private readonly ShoppingCartItem _shoppingCartItem;

    public LiquidShoppingCartItem(Product product,
        string attributeDescription,
        string pictureUrl,
        ShoppingCartItem shoppingCartItem,
        Language language)
    {
        _language = language;
        _product = product;
        AttributeDescription = attributeDescription;
        PictureUrl = pictureUrl;
        _shoppingCartItem = shoppingCartItem;
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string AttributeDescription { get; }

    public string PictureUrl { get; }

    public int Quantity => _shoppingCartItem.Quantity;

    public ShoppingCartType ShoppingCartType => _shoppingCartItem.ShoppingCartTypeId;

    public string ProductName {
        get {
            var name = "";

            if (_product != null)
                name = WebUtility.HtmlEncode(_product.GetTranslation(x => x.Name, _language.Id));

            return name;
        }
    }

    public string ProductSeName {
        get {
            var name = "";

            if (_product != null)
                name = _product.GetTranslation(x => x.SeName, _language.Id);
            return name;
        }
    }

    public string ProductShortDescription {
        get {
            var desc = "";

            if (_product != null)
                desc = WebUtility.HtmlEncode(_product.GetTranslation(x => x.ShortDescription, _language.Id));

            return desc;
        }
    }

    public string ProductFullDescription {
        get {
            var desc = "";

            if (_product != null)
                desc = WebUtility.HtmlDecode(_product.GetTranslation(x => x.FullDescription, _language.Id));

            return desc;
        }
    }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}