using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Extensions
{
    public static class ProductExtensions
    {
        /// <summary>
        /// Used to get an appropriate tier price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store id</param>
        /// <param name="quantity">Quantity</param>
        /// <returns>Price</returns>
        public static TierPrice GetPreferredTierPrice(this Product product, Customer customer, string storeId, string currencyCode, int quantity)
        {
            if (!product.TierPrices.Any())
                return null;

            var actualTierPrices = product.TierPrices.OrderBy(price => price.Quantity).ToList()
                .FilterByStore(storeId)
                .FilterByCurrency(currencyCode)
                .FilterForCustomer(customer)
                .FilterByDate()
                .RemoveDuplicatedQuantities();

            var tierPrice = actualTierPrices.LastOrDefault(price => quantity >= price.Quantity);

            return tierPrice;
        }

        /// <summary>
        /// Check if product tag exists
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="productTagId">Product tag id</param>
        /// <returns>Result</returns>
        public static bool ProductTagExists(this Product product,
            string productTagName)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            bool result = product.ProductTags.FirstOrDefault(pt => pt == productTagName) != null;
            return result;
        }

        /// <summary>
        /// List of allowed quantities
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>Result</returns>
        public static int[] ParseAllowedQuantities(this Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var result = new List<int>();
            if (!String.IsNullOrWhiteSpace(product.AllowedQuantities))
            {
                product.AllowedQuantities
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList()
                    .ForEach(qtyStr =>
                    {
                        int qty;
                        if (int.TryParse(qtyStr.Trim(), out qty))
                        {
                            result.Add(qty);
                        }
                    });
            }

            return result.ToArray();
        }

        /// <summary>
        /// Gets SKU, Collection part number and GTIN
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="productAttributeParser">Product attribute service</param>
        /// <param name="sku">SKU</param>
        /// <param name="Mpn">MPN</param>
        /// <param name="gtin">GTIN</param>
        private static void GetSkuMpnGtin(this Product product, IList<CustomAttribute> attributes, IProductAttributeParser productAttributeParser,
            out string sku, out string Mpn, out string gtin)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            sku = null;
            Mpn = null;
            gtin = null;

            if (attributes != null &&
                product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            {
                if (productAttributeParser == null)
                    throw new ArgumentNullException(nameof(productAttributeParser));

                var combination = productAttributeParser.FindProductAttributeCombination(product, attributes);
                if (combination != null)
                {
                    sku = combination.Sku;
                    Mpn = combination.Mpn;
                    gtin = combination.Gtin;
                }
            }

            if (string.IsNullOrEmpty(sku))
                sku = product.Sku;
            if (string.IsNullOrEmpty(Mpn))
                Mpn = product.Mpn;
            if (string.IsNullOrEmpty(gtin))
                gtin = product.Gtin;
        }

        /// <summary>
        /// SKU
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="productAttributeParser">Product attribute service</param>
        /// <returns>SKU</returns>
        public static string FormatSku(this Product product, IList<CustomAttribute> attributes = null, IProductAttributeParser productAttributeParser = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            string sku;

            product.GetSkuMpnGtin(attributes, productAttributeParser,
                out sku, out _, out _);

            return sku;
        }

        /// <summary>
        /// MPN
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="productAttributeParser">Product attribute service</param>
        /// <returns>Collection part number</returns>
        public static string FormatMpn(this Product product, IList<CustomAttribute> attributes = null, IProductAttributeParser productAttributeParser = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            string Mpn;

            product.GetSkuMpnGtin(attributes, productAttributeParser,
                out _, out Mpn, out _);

            return Mpn;
        }

        /// <summary>
        /// GTIN
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="productAttributeParser">Product attribute service</param>
        /// <returns>GTIN</returns>
        public static string FormatGtin(this Product product, IList<CustomAttribute> attributes = null, IProductAttributeParser productAttributeParser = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            string gtin;

            product.GetSkuMpnGtin(attributes, productAttributeParser,
                out _, out _, out gtin);

            return gtin;
        }

        /// <summary>
        /// Get product picture (for shopping cart and order details pages)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributes">Atributes </param>
        /// <param name="pictureService">Picture service</param>
        /// <param name="productAttributeParser">Product attribute service</param>
        /// <returns>Picture</returns>
        public static async Task<Picture> GetProductPicture(this Product product, IList<CustomAttribute> attributes,
            IProductService productService, IPictureService pictureService,
            IProductAttributeParser productAttributeParser)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));
            if (pictureService == null)
                throw new ArgumentNullException(nameof(pictureService));
            if (productAttributeParser == null)
                throw new ArgumentNullException(nameof(productAttributeParser));

            Picture picture = null;

            if (attributes != null && attributes.Any())
            {

                var comb = productAttributeParser.FindProductAttributeCombination(product, attributes);
                if (comb != null)
                {
                    if (!string.IsNullOrEmpty(comb.PictureId))
                    {
                        var combPicture = await pictureService.GetPictureById(comb.PictureId);
                        if (combPicture != null)
                        {
                            picture = combPicture;
                        }
                    }
                }
                if (picture == null)
                {
                    var attributeValues = productAttributeParser.ParseProductAttributeValues(product, attributes);
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributePicture = await pictureService.GetPictureById(attributeValue.PictureId);
                        if (attributePicture != null)
                        {
                            picture = attributePicture;
                            break;
                        }
                    }
                }
            }
            if (picture == null)
            {
                var pp = product.ProductPictures.OrderBy(x => x.DisplayOrder).FirstOrDefault();
                if (pp != null)
                    picture = await pictureService.GetPictureById(pp.PictureId);
            }

            if (picture == null && !product.VisibleIndividually && !string.IsNullOrEmpty(product.ParentGroupedProductId))
            {
                var parentProduct = await productService.GetProductById(product.ParentGroupedProductId);
                if (parentProduct != null)
                    if (parentProduct.ProductPictures.Any())
                    {
                        picture = await pictureService.GetPictureById(parentProduct.ProductPictures.FirstOrDefault().PictureId);

                    }
            }

            return picture;
        }
    }
}
