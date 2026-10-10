using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Seo;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Services.ExportImport;

/// <summary>
///     Row API only: assigns catalogue product attributes to an existing product (mappings and their values) and
///     creates or updates an explicit list of attribute combinations. Nothing is deleted; the product is never created.
///     A dry run evaluates each row against the stored product only: a later row for the same product does not see
///     the mappings or values an earlier row of the same dry run would add, so send one row per product
/// </summary>
public class ProductVariantImportDataObject : IRowImport<ProductVariantsDto>
{
    public const int MaxCombinationsPerProduct = 50;

    private readonly ImportHtmlGuard _htmlGuard;
    private readonly ILogger<ProductVariantImportDataObject> _logger;
    private readonly IProductAttributeService _productAttributeService;
    private readonly IProductService _productService;
    private readonly SeoSettings _seoSettings;

    public ProductVariantImportDataObject(IProductService productService,
        IProductAttributeService productAttributeService, SeoSettings seoSettings, ImportHtmlGuard htmlGuard,
        ILogger<ProductVariantImportDataObject> logger)
    {
        _logger = logger;
        _productService = productService;
        _productAttributeService = productAttributeService;
        _seoSettings = seoSettings;
        _htmlGuard = htmlGuard;
    }

    /// <summary>
    ///     The writes of one row are not atomic: a row that fails part way comes back with
    ///     <see cref="ImportRowResult.FailedError" />. Sending it again is safe - mappings, values and combinations are
    ///     matched, not added twice - and completes it
    /// </summary>
    public Task<ImportBatchResult> Import(IReadOnlyList<ProductVariantsDto> rows, bool dryRun,
        CancellationToken cancellationToken = default)
    {
        return ImportRows.RunBatch(rows, dryRun, r => r.ProductId, Key, (row, dto) => ImportRow(row, dto, dryRun),
            true, _logger, cancellationToken);
    }

    private async Task<ImportRowResult> ImportRow(int row, ProductVariantsDto dto, bool dryRun)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var key = Key(dto);
        var attributeDtos = dto.Attributes?.Where(a => a != null).ToList() ?? [];
        var combinationDtos = dto.Combinations?.Where(c => c != null).ToList() ?? [];

        var product = await FindProduct(dto, errors);

        if (combinationDtos.Count > MaxCombinationsPerProduct)
            errors.Add(
                $"At most {MaxCombinationsPerProduct} combinations per product; the row has {combinationDtos.Count}.");

        foreach (var attributeDto in attributeDtos)
        {
            if (string.IsNullOrEmpty(attributeDto.Attribute))
                errors.Add("Attribute is required.");
            if (attributeDto.ControlType.HasValue && !Enum.IsDefined(attributeDto.ControlType.Value))
                errors.Add($"ControlType '{attributeDto.ControlType}' is not valid.");

            foreach (var value in attributeDto.Values ?? [])
            {
                if (string.IsNullOrEmpty(value?.Name))
                {
                    errors.Add("Value Name is required.");
                    continue;
                }

                errors.AddRange(_htmlGuard.PlainTextErrors(("Value Name", value.Name),
                    (nameof(value.ColorSquaresRgb), value.ColorSquaresRgb)));
            }
        }

        foreach (var combination in combinationDtos)
        {
            if (combination.Values?.Keys.Any(string.IsNullOrEmpty) == true)
                errors.Add("Combination attribute is required.");
            errors.AddRange(_htmlGuard.PlainTextErrors((nameof(combination.Sku), combination.Sku),
                (nameof(combination.Gtin), combination.Gtin), (nameof(combination.Mpn), combination.Mpn)));
        }

        var attributes = await ResolveAttributes(attributeDtos, combinationDtos, errors);

        if (errors.Count > 0 || product == null)
            return new ImportRowResult(row, ImportRowStatus.Rejected, product?.Id ?? "", key, errors, warnings);

        //the product was read from the database, not the cache, so a dry run can change it in memory freely
        var changes = new Changes();
        foreach (var attributeDto in attributeDtos)
            MergeMapping(product, attributes[attributeDto.Attribute], attributeDto, changes, warnings);

        //the storefront matches a combination only when it names every selectable, combinable mapping
        var combinable = product.ProductAttributeMappings.Where(m => IsSelectable(m) && m.Combination).ToList();
        var names = await MappingNames(combinable, attributes);
        for (var i = 0; i < combinationDtos.Count; i++)
        {
            var pairs = MergeCombination(product, i + 1, combinationDtos[i], attributes, changes, warnings);
            if (pairs == null)
                continue;
            foreach (var missing in combinable.Where(m => pairs.All(p => p.Key != m.Id)))
                warnings.Add(
                    $"Combination {i + 1} does not cover '{names[missing.Id]}'; the storefront will not match it.");
        }

        if (!dryRun)
            await Save(product.Id, changes);

        return new ImportRowResult(row, ImportRowStatus.Updated, product.Id, key, errors, warnings);
    }

    private async Task<Product> FindProduct(ProductVariantsDto dto, List<string> errors)
    {
        if (!string.IsNullOrEmpty(dto.ProductId))
        {
            var byId = await _productService.GetProductById(dto.ProductId, true);
            if (byId == null)
                errors.Add($"Product '{dto.ProductId}' was not found.");
            return byId;
        }

        if (string.IsNullOrEmpty(dto.ProductSku))
        {
            errors.Add("ProductId or ProductSku is required.");
            return null;
        }

        //read from the database, not the cache
        var bySku = await _productService.GetProductBySku(dto.ProductSku);
        if (bySku == null)
            errors.Add($"Product with SKU '{dto.ProductSku}' was not found.");
        return bySku;
    }

    /// <summary>Every attribute key the row uses (mappings and combinations), by id else by normalized SeName</summary>
    private async Task<Dictionary<string, ProductAttribute>> ResolveAttributes(
        List<ProductVariantAttributeDto> attributeDtos, List<ProductVariantCombinationDto> combinationDtos,
        List<string> errors)
    {
        var keys = attributeDtos.Select(a => a.Attribute)
            .Concat(combinationDtos.SelectMany(c => c.Values?.Keys ?? []))
            .Where(k => !string.IsNullOrEmpty(k))
            .Distinct();

        var resolved = new Dictionary<string, ProductAttribute>();
        IList<ProductAttribute> all = null;
        foreach (var attributeKey in keys)
        {
            var attribute = await _productAttributeService.GetProductAttributeById(attributeKey);
            if (attribute == null)
            {
                //no lookup by SeName exists: load all once and match
                all ??= await _productAttributeService.GetAllProductAttributes();
                var seName = GetSeName(attributeKey);
                attribute = string.IsNullOrEmpty(seName)
                    ? null
                    : all.FirstOrDefault(a => string.Equals(a.SeName, seName, StringComparison.OrdinalIgnoreCase));
            }

            if (attribute == null)
                errors.Add($"Attribute '{attributeKey}' was not found.");
            else
                resolved[attributeKey] = attribute;
        }

        return resolved;
    }

    /// <summary>Attribute name of each mapping, for warnings: from the row's attributes, else from the catalogue</summary>
    private async Task<Dictionary<string, string>> MappingNames(List<ProductAttributeMapping> mappings,
        Dictionary<string, ProductAttribute> attributes)
    {
        var names = new Dictionary<string, string>();
        foreach (var mapping in mappings)
        {
            var attribute = attributes.Values.FirstOrDefault(a => a.Id == mapping.ProductAttributeId) ??
                            await _productAttributeService.GetProductAttributeById(mapping.ProductAttributeId);
            names[mapping.Id] = attribute?.Name ?? mapping.ProductAttributeId;
        }

        return names;
    }

    private static void MergeMapping(Product product, ProductAttribute attribute, ProductVariantAttributeDto dto,
        Changes changes, List<string> warnings)
    {
        var mapping = product.ProductAttributeMappings.FirstOrDefault(m => m.ProductAttributeId == attribute.Id);
        if (mapping == null)
        {
            mapping = new ProductAttributeMapping {
                ProductAttributeId = attribute.Id,
                AttributeControlTypeId = dto.ControlType ?? AttributeControlType.DropdownList,
                IsRequired = dto.IsRequired ?? true,
                Combination = dto.Combination ?? true,
                DisplayOrder = dto.DisplayOrder ??
                               NextDisplayOrder(product.ProductAttributeMappings.Select(m => m.DisplayOrder))
            };
            product.ProductAttributeMappings.Add(mapping);
            changes.NewMappings.Add(mapping);
        }
        else
        {
            var wasSelectable = IsSelectable(mapping);
            var oldControlType = mapping.AttributeControlTypeId;
            //an omitted control type keeps the stored one; only an explicit, different one changes it
            var changed = Assign(dto.ControlType, mapping.AttributeControlTypeId,
                v => mapping.AttributeControlTypeId = v);
            if (changed)
                warnings.Add($"Mapping '{attribute.Name}' changed from {oldControlType} to {mapping.AttributeControlTypeId}.");
            if (wasSelectable && !IsSelectable(mapping) &&
                product.ProductAttributeCombinations.Any(c => c.Attributes.Any(a => a.Key == mapping.Id)))
                warnings.Add(
                    $"Mapping '{attribute.Name}' is no longer selectable; its combinations cannot be chosen in the storefront.");
            changed |= Assign(dto.IsRequired, mapping.IsRequired, v => mapping.IsRequired = v);
            changed |= Assign(dto.Combination, mapping.Combination, v => mapping.Combination = v);
            changed |= Assign(dto.DisplayOrder, mapping.DisplayOrder, v => mapping.DisplayOrder = v);
            if (changed && !changes.NewMappings.Contains(mapping) && !changes.UpdatedMappings.Contains(mapping))
                changes.UpdatedMappings.Add(mapping);
        }

        //a repeated name in the same row matches the value just added, so it collapses into it
        foreach (var valueDto in dto.Values ?? [])
        {
            var value = mapping.ProductAttributeValues
                .FirstOrDefault(v => string.Equals(v.Name, valueDto.Name, StringComparison.OrdinalIgnoreCase));
            var isNew = value == null;
            if (isNew)
            {
                value = new ProductAttributeValue {
                    Name = valueDto.Name,
                    DisplayOrder = NextDisplayOrder(mapping.ProductAttributeValues.Select(v => v.DisplayOrder))
                };
                mapping.ProductAttributeValues.Add(value);
            }

            var changed = Assign(valueDto.PriceAdjustment, value.PriceAdjustment, v => value.PriceAdjustment = v);
            changed |= Assign(valueDto.WeightAdjustment, value.WeightAdjustment, v => value.WeightAdjustment = v);
            changed |= Assign(valueDto.Cost, value.Cost, v => value.Cost = v);
            changed |= Assign(valueDto.IsPreSelected, value.IsPreSelected, v => value.IsPreSelected = v);
            changed |= Assign(valueDto.DisplayOrder, value.DisplayOrder, v => value.DisplayOrder = v);
            changed |= Assign(valueDto.ColorSquaresRgb, value.ColorSquaresRgb, v => value.ColorSquaresRgb = v);

            //the values of a new mapping are inserted with it
            if (changes.NewMappings.Contains(mapping))
                continue;
            if (isNew)
                changes.NewValues.Add((mapping, value));
            else if (changed && !changes.NewValues.Any(n => n.Value == value) &&
                     !changes.UpdatedValues.Any(u => u.Value == value))
                changes.UpdatedValues.Add((mapping, value));
        }
    }

    /// <returns>The (mapping, value) pairs of the combination, or null when it was skipped</returns>
    private static List<CustomAttribute> MergeCombination(Product product, int number,
        ProductVariantCombinationDto dto,
        Dictionary<string, ProductAttribute> attributes, Changes changes, List<string> warnings)
    {
        var prefix = $"Combination {number}";
        if (dto.Values == null || dto.Values.Count == 0)
        {
            warnings.Add($"{prefix} was skipped: Values are required.");
            return null;
        }

        if (dto.Price < 0)
        {
            warnings.Add($"{prefix} was skipped: Price cannot be negative.");
            return null;
        }

        //stored as the panel stores it: one (mapping id, value id) pair per attribute
        var pairs = new List<CustomAttribute>();
        foreach (var (attributeKey, valueName) in dto.Values)
        {
            var attribute = attributes[attributeKey];
            var mapping = product.ProductAttributeMappings.FirstOrDefault(m => m.ProductAttributeId == attribute.Id);
            if (mapping == null)
            {
                warnings.Add($"{prefix} was skipped: attribute '{attribute.Name}' is not assigned to the product.");
                return null;
            }

            if (!IsSelectable(mapping))
            {
                warnings.Add(
                    $"{prefix} was skipped: attribute '{attribute.Name}' is a {mapping.AttributeControlTypeId} attribute and has no values to combine.");
                return null;
            }

            //the storefront ignores such a mapping when it looks a combination up, so it would never be found
            if (!mapping.Combination)
            {
                warnings.Add(
                    $"{prefix} was skipped: attribute '{attribute.Name}' does not take part in combinations (Combination is off).");
                return null;
            }

            if (pairs.Any(p => p.Key == mapping.Id))
            {
                warnings.Add($"{prefix} was skipped: attribute '{attribute.Name}' is named more than once.");
                return null;
            }

            var value = string.IsNullOrEmpty(valueName)
                ? null
                : mapping.ProductAttributeValues.FirstOrDefault(v =>
                    string.Equals(v.Name, valueName, StringComparison.OrdinalIgnoreCase));
            if (value == null)
            {
                warnings.Add($"{prefix} was skipped: attribute '{attribute.Name}' has no value '{valueName}'.");
                return null;
            }

            pairs.Add(new CustomAttribute { Key = mapping.Id, Value = value.Id });
        }

        var combination = product.ProductAttributeCombinations.FirstOrDefault(c => SamePairs(c.Attributes, pairs));

        if (!string.IsNullOrEmpty(dto.Sku) && product.ProductAttributeCombinations.Any(c =>
                c != combination && string.Equals(c.Sku, dto.Sku, StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add(
                $"{prefix} was skipped: SKU '{dto.Sku}' is already used by another combination of this product.");
            return null;
        }

        if (combination == null)
        {
            //same defaults as the panel's "generate all combinations"
            combination = new ProductAttributeCombination {
                Attributes = pairs,
                Sku = dto.Sku,
                Gtin = dto.Gtin,
                Mpn = dto.Mpn,
                OverriddenPrice = dto.Price,
                StockQuantity = 0,
                AllowOutOfStockOrders = false,
                NotifyAdminForQuantityBelow = 1
            };
            product.ProductAttributeCombinations.Add(combination);
            changes.NewCombinations.Add(combination);
            return pairs;
        }

        var changed = Assign(dto.Sku, combination.Sku, v => combination.Sku = v);
        changed |= Assign(dto.Gtin, combination.Gtin, v => combination.Gtin = v);
        changed |= Assign(dto.Mpn, combination.Mpn, v => combination.Mpn = v);
        if (dto.Price.HasValue && combination.OverriddenPrice != dto.Price)
        {
            combination.OverriddenPrice = dto.Price;
            changed = true;
        }

        if (changed && !changes.NewCombinations.Contains(combination) &&
            !changes.UpdatedCombinations.Contains(combination))
            changes.UpdatedCombinations.Add(combination);

        return pairs;
    }

    private async Task Save(string productId, Changes changes)
    {
        foreach (var mapping in changes.NewMappings)
            await _productAttributeService.InsertProductAttributeMapping(mapping, productId);
        //an update writes the whole mapping, its values included
        foreach (var mapping in changes.UpdatedMappings)
            await _productAttributeService.UpdateProductAttributeMapping(mapping, productId);
        foreach (var (mapping, value) in changes.NewValues.Where(v => !changes.UpdatedMappings.Contains(v.Mapping)))
            await _productAttributeService.InsertProductAttributeValue(value, productId, mapping.Id);
        foreach (var (mapping, value) in changes.UpdatedValues.Where(v => !changes.UpdatedMappings.Contains(v.Mapping)))
            await _productAttributeService.UpdateProductAttributeValue(value, productId, mapping.Id);
        foreach (var combination in changes.NewCombinations)
            await _productAttributeService.InsertProductAttributeCombination(combination, productId);
        foreach (var combination in changes.UpdatedCombinations)
            await _productAttributeService.UpdateProductAttributeCombination(combination, productId);
    }

    /// <summary>TextBox, MultilineTextbox, Datepicker, FileUpload and Hidden have no values a shopper selects</summary>
    private static bool IsSelectable(ProductAttributeMapping mapping)
    {
        return mapping.ShouldHaveValues() && mapping.AttributeControlTypeId != AttributeControlType.Hidden;
    }

    private static bool SamePairs(IList<CustomAttribute> stored, List<CustomAttribute> pairs)
    {
        if (stored == null || stored.Count != pairs.Count)
            return false;

        var set = stored.Select(a => (a.Key, a.Value)).ToHashSet();
        return set.Count == pairs.Count && pairs.All(p => set.Contains((p.Key, p.Value)));
    }

    private static int NextDisplayOrder(IEnumerable<int> orders)
    {
        var list = orders.ToList();
        return list.Count == 0 ? 0 : list.Max() + 1;
    }

    private static bool Assign<T>(T? provided, T current, Action<T> set) where T : struct
    {
        if (!provided.HasValue || EqualityComparer<T>.Default.Equals(provided.Value, current))
            return false;

        set(provided.Value);
        return true;
    }

    private static bool Assign(string provided, string current, Action<string> set)
    {
        if (provided == null || provided == current)
            return false;

        set(provided);
        return true;
    }

    private static string Key(ProductVariantsDto dto)
    {
        return !string.IsNullOrEmpty(dto.ProductSku) ? dto.ProductSku : dto.ProductId ?? "";
    }

    //same normalization as the product attribute import
    private string GetSeName(string name)
    {
        return SeoExtensions.GetSeName(name, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls,
            _seoSettings.SeoCharConversion);
    }

    private sealed class Changes
    {
        public List<ProductAttributeMapping> NewMappings { get; } = [];
        public List<ProductAttributeMapping> UpdatedMappings { get; } = [];
        public List<(ProductAttributeMapping Mapping, ProductAttributeValue Value)> NewValues { get; } = [];
        public List<(ProductAttributeMapping Mapping, ProductAttributeValue Value)> UpdatedValues { get; } = [];
        public List<ProductAttributeCombination> NewCombinations { get; } = [];
        public List<ProductAttributeCombination> UpdatedCombinations { get; } = [];
    }
}
