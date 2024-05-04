using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.SharedKernel.Extensions;

namespace Grand.Domain.Catalog;

/// <summary>
///     Product extensions
/// </summary>
public static class ProductExtensions
{
    /// <summary>
    ///     Used to get an appropriate tier price
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customer">Customer</param>
    /// <param name="storeId">Store id</param>
    /// <param name="currencyCode"></param>
    /// <param name="quantity">Quantity</param>
    /// <returns>Price</returns>
    public static TierPrice GetPreferredTierPrice(this Product product, Customer customer, string storeId,
        string currencyCode, int quantity)
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
    ///     Check if product tag exists
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="productTagName"></param>
    /// <returns>Result</returns>
    public static bool ProductTagExists(this Product product,
        string productTagName)
    {
        ArgumentNullException.ThrowIfNull(product);

        var result = product.ProductTags.FirstOrDefault(pt => pt == productTagName) != null;
        return result;
    }

    /// <summary>
    ///     List of allowed quantities
    /// </summary>
    /// <param name="product">Product</param>
    /// <returns>Result</returns>
    public static int[] ParseAllowedQuantities(this Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var result = new List<int>();
        if (!string.IsNullOrWhiteSpace(product.AllowedQuantities))
            product.AllowedQuantities
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList()
                .ForEach(qtyStr =>
                {
                    int qty;
                    if (int.TryParse(qtyStr.Trim(), out qty)) result.Add(qty);
                });

        return result.ToArray();
    }

    /// <summary>
    ///     Gets SKU, Collection part number and GTIN
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="attributes">Attributes</param>
    /// <param name="sku">SKU</param>
    /// <param name="mpn">MPN</param>
    /// <param name="gtin">GTIN</param>
    private static void GetSkuMpnGtin(this Product product, IList<CustomAttribute> attributes,
        out string sku, out string mpn, out string gtin)
    {
        ArgumentNullException.ThrowIfNull(product);

        sku = null;
        mpn = null;
        gtin = null;

        if (attributes != null && attributes.Any())
        {
            var combination = FindProductAttributeCombination(product, attributes);
            if (combination != null)
            {
                sku = combination.Sku;
                mpn = combination.Mpn;
                gtin = combination.Gtin;
            }
        }

        if (string.IsNullOrEmpty(sku))
            sku = product.Sku;
        if (string.IsNullOrEmpty(mpn))
            mpn = product.Mpn;
        if (string.IsNullOrEmpty(gtin))
            gtin = product.Gtin;
    }

    /// <summary>
    ///     Finds a product attribute combination by attributes stored
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="ignoreNonCombinableAttributes">A value indicating whether we should ignore non-combinable attributes</param>
    /// <returns>Found product attribute combination</returns>
    public static ProductAttributeCombination FindProductAttributeCombination(this Product product,
        IList<CustomAttribute> customAttributes, bool ignoreNonCombinableAttributes = true)
    {
        ArgumentNullException.ThrowIfNull(product);

        var combinations = product.ProductAttributeCombinations;
        return combinations.FirstOrDefault(x =>
            AreProductAttributesEqual(product, x.Attributes, customAttributes, ignoreNonCombinableAttributes));
    }


    /// <summary>
    ///     Gets selected product attribute mappings
    /// </summary>
    /// <param name="product"></param>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Selected product attribute mappings</returns>
    public static IList<ProductAttributeMapping> ParseProductAttributeMappings(this Product product,
        IList<CustomAttribute> customAttributes)
    {
        var result = new List<ProductAttributeMapping>();
        if (customAttributes == null || !customAttributes.Any())
            return result;

        foreach (var customAttribute in customAttributes.GroupBy(x => x.Key))
        {
            var attribute = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == customAttribute.Key);
            if (attribute != null) result.Add(attribute);
        }

        return result;
    }

    /// <summary>
    ///     Get product attribute values
    /// </summary>
    /// <param name="product"></param>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Product attribute values</returns>
    public static IList<ProductAttributeValue> ParseProductAttributeValues(this Product product,
        IList<CustomAttribute> customAttributes)
    {
        var values = new List<ProductAttributeValue>();
        if (customAttributes == null || !customAttributes.Any())
            return values;

        var attributes = ParseProductAttributeMappings(product, customAttributes);
        foreach (var attribute in attributes)
        {
            if (!attribute.ShouldHaveValues())
                continue;

            var valuesStr = ParseValues(customAttributes, attribute.Id);
            foreach (var valueStr in valuesStr)
                if (!string.IsNullOrEmpty(valueStr))
                    if (attribute.ProductAttributeValues.Any(x => x.Id == valueStr))
                    {
                        var value = attribute.ProductAttributeValues.FirstOrDefault(x => x.Id == valueStr);
                        if (value != null) values.Add(value);
                    }
        }

        return values;
    }

    /// <summary>
    ///     Gets selected product attribute values
    /// </summary>
    /// <param name="customAttributes">Attributes </param>
    /// <param name="productAttributeMappingId">Product attribute mapping identifier</param>
    /// <returns>Product attribute values</returns>
    public static IList<string> ParseValues(IList<CustomAttribute> customAttributes, string productAttributeMappingId)
    {
        var selectedValues = new List<string>();
        if (customAttributes == null || !customAttributes.Any())
            return selectedValues;

        return customAttributes.Where(x => x.Key == productAttributeMappingId).Select(x => x.Value).ToList();
    }

    /// <summary>
    ///     Adds an attribute
    /// </summary>
    /// <param name="customAttributes"></param>
    /// <param name="productAttributeMapping">Product attribute mapping</param>
    /// <param name="value">Value</param>
    /// <returns>Attributes</returns>
    public static IList<CustomAttribute> AddProductAttribute(IList<CustomAttribute> customAttributes,
        ProductAttributeMapping productAttributeMapping, string value)
    {
        customAttributes ??= new List<CustomAttribute>();
        customAttributes.Add(new CustomAttribute { Key = productAttributeMapping.Id, Value = value });

        return customAttributes;
    }

    /// <summary>
    ///     Remove an attribute
    /// </summary>
    /// <param name="customAttributes"></param>
    /// <param name="productAttributeMapping">Product attribute mapping</param>
    /// <returns>Updated result (XML format)</returns>
    public static IList<CustomAttribute> RemoveProductAttribute(IList<CustomAttribute> customAttributes,
        ProductAttributeMapping productAttributeMapping)
    {
        return customAttributes.Where(x => x.Key != productAttributeMapping.Id).ToList();
    }

    /// <summary>
    ///     Are attributes equal
    /// </summary>
    /// <param name="product"></param>
    /// <param name="customAttributes1">The attributes of the first product</param>
    /// <param name="customAttributes2">The attributes of the second product</param>
    /// <param name="ignoreNonCombinableAttributes">A value indicating whether we should ignore non-combinable attributes</param>
    /// <returns>Result</returns>
    public static bool AreProductAttributesEqual(Product product, IList<CustomAttribute> customAttributes1,
        IList<CustomAttribute> customAttributes2, bool ignoreNonCombinableAttributes)
    {
        var attributes1 = ParseProductAttributeMappings(product, customAttributes1);
        if (ignoreNonCombinableAttributes) attributes1 = attributes1.Where(x => !x.IsNonCombinable()).ToList();
        var attributes2 = ParseProductAttributeMappings(product, customAttributes2);
        //TO DO - Where(x=>x.IsRequired).ToList()

        if (ignoreNonCombinableAttributes) attributes2 = attributes2.Where(x => !x.IsNonCombinable()).ToList();
        if (attributes1.Count != attributes2.Count)
            return false;

        var attributesEqual = true;
        foreach (var a1 in attributes1)
        {
            var hasAttribute = false;
            foreach (var a2 in attributes2)
                if (a1.Id == a2.Id)
                {
                    hasAttribute = true;
                    var values1Str = ParseValues(customAttributes1, a1.Id);
                    var values2Str = ParseValues(customAttributes2, a2.Id);
                    if (values1Str.Count == values2Str.Count)
                    {
                        foreach (var str1 in values1Str)
                        {
                            var hasValue = false;
                            foreach (var str2 in values2Str)
                                //case insensitive? 
                                if (str1.Trim() == str2.Trim())
                                {
                                    hasValue = true;
                                    break;
                                }

                            if (!hasValue)
                            {
                                attributesEqual = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        attributesEqual = false;
                        break;
                    }
                }

            if (hasAttribute == false)
            {
                attributesEqual = false;
                break;
            }
        }

        return attributesEqual;
    }

    /// <summary>
    ///     Check whether condition of some attribute is met (if specified). Return "null" if not condition is specified
    /// </summary>
    /// <param name="product"></param>
    /// <param name="pam">Product attribute</param>
    /// <param name="selectedAttributes">Selected attributes</param>
    /// <returns>Result</returns>
    public static bool? IsConditionMet(this Product product, ProductAttributeMapping pam,
        IList<CustomAttribute> selectedAttributes)
    {
        ArgumentNullException.ThrowIfNull(pam);

        selectedAttributes ??= new List<CustomAttribute>();

        var conditionAttribute = pam.ConditionAttribute;
        if (!conditionAttribute.Any())
            //no condition
            return null;

        //load an attribute this one depends on
        var dependOnAttribute = ParseProductAttributeMappings(product, conditionAttribute).FirstOrDefault();
        if (dependOnAttribute == null)
            return true;

        var valuesThatShouldBeSelected = ParseValues(conditionAttribute, dependOnAttribute.Id)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();
        var selectedValues = ParseValues(selectedAttributes, dependOnAttribute.Id);
        if (valuesThatShouldBeSelected.Count != selectedValues.Count)
            return false;

        //compare values
        var allFound = true;
        foreach (var t1 in valuesThatShouldBeSelected)
        {
            var found = false;
            foreach (var t2 in selectedValues)
                if (t1 == t2)
                    found = true;
            if (!found)
                allFound = false;
        }

        return allFound;
    }

    /// <summary>
    ///     Generate all combinations
    /// </summary>
    /// <param name="product">Product</param>
    /// <returns>Attribute combinations</returns>
    public static IList<IEnumerable<CustomAttribute>> GenerateAllCombinations(this Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var allProductAttributMappings = product.ProductAttributeMappings.Where(x => !x.IsNonCombinable()).ToList();

        if (allProductAttributMappings.Count == 0)
            return null;

        var query = allProductAttributMappings.Select(o1 => o1.ProductAttributeValues.Select(o2 =>
            new CustomAttribute {
                Key = o1.Id,
                Value = o2.Id
            }
        )).SelectMany(x => x).ToList();

        var result = query.GroupBy(t => t.Key).CartesianProduct().ToList();

        return result;
    }

    /// <summary>
    ///     SKU
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="attributes">Attributes</param>
    /// <returns>SKU</returns>
    public static string FormatSku(this Product product, IList<CustomAttribute> attributes = null)
    {
        ArgumentNullException.ThrowIfNull(product);

        product.GetSkuMpnGtin(attributes, out var sku, out _, out _);

        return sku;
    }

    /// <summary>
    ///     MPN
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="attributes">Attributes</param>
    /// <returns>Collection part number</returns>
    public static string FormatMpn(this Product product, IList<CustomAttribute> attributes = null)
    {
        ArgumentNullException.ThrowIfNull(product);

        product.GetSkuMpnGtin(attributes, out _, out var Mpn, out _);

        return Mpn;
    }

    /// <summary>
    ///     GTIN
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="attributes">Attributes</param>
    /// <returns>GTIN</returns>
    public static string FormatGtin(this Product product, IList<CustomAttribute> attributes = null)
    {
        ArgumentNullException.ThrowIfNull(product);

        product.GetSkuMpnGtin(attributes, out _, out _, out var gtin);

        return gtin;
    }

    /// <summary>
    ///     Parse "required product Ids" property
    /// </summary>
    /// <param name="product">Product</param>
    /// <returns>A list of required product IDs</returns>
    public static string[] ParseRequiredProductIds(this Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrEmpty(product.RequiredProductIds))
            return Array.Empty<string>();

        var ids = new List<string>();

        foreach (var idStr in product.RequiredProductIds
                     .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(x => x.Trim()))
            ids.Add(idStr);

        return ids.ToArray();
    }

    /// <summary>
    ///     Get a value indicating whether a product is available now (availability dates)
    /// </summary>
    /// <param name="product">Product</param>
    /// <returns>Result</returns>
    public static bool IsAvailable(this Product product)
    {
        return IsAvailable(product, DateTime.UtcNow);
    }

    /// <summary>
    ///     Get a value indicating whether a product is available now (availability dates)
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="dateTime">Datetime to check</param>
    /// <returns>Result</returns>
    public static bool IsAvailable(this Product product, DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (product.AvailableStartDateTimeUtc.HasValue && product.AvailableStartDateTimeUtc.Value > dateTime)
            return false;

        if (product.AvailableEndDateTimeUtc.HasValue && product.AvailableEndDateTimeUtc.Value < dateTime) return false;

        return true;
    }
}