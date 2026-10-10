using Grand.Business.Catalog.Services.ExportImport;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Security;
using Grand.Infrastructure.Tests.Caching;
using Microsoft.Extensions.Logging;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.ExportImport;

/// <summary>Runs against the real ProductAttributeService over the test Mongo database</summary>
[TestClass]
public class ProductVariantImportDataObjectTests
{
    private Mock<ProductAttributeService> _attributeService;
    private ProductAttribute _color;
    private ProductVariantImportDataObject _import;
    private Product _product;
    private IRepository<Product> _productRepository;
    private Mock<IProductService> _productService;
    private ProductAttribute _size;

    [TestInitialize]
    public async Task Init()
    {
        _productRepository = new MongoDBRepositoryTest<Product>();
        var attributeRepository = new MongoDBRepositoryTest<ProductAttribute>();
        var mediator = new Mock<IMediator>();
        var cache = new MemoryCacheBase(MemoryCacheTest.Get(), mediator.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _attributeService =
            new Mock<ProductAttributeService>(cache, attributeRepository, _productRepository, mediator.Object) {
                CallBase = true
            };

        _color = new ProductAttribute { Name = "Color", SeName = "color" };
        _size = new ProductAttribute { Name = "Shirt Size", SeName = "shirt-size" };
        await attributeRepository.InsertAsync(_color);
        await attributeRepository.InsertAsync(_size);

        _product = new Product { Name = "Shirt", Sku = "SHIRT" };
        await _productRepository.InsertAsync(_product);

        _productService = new Mock<IProductService>();
        _productService.Setup(s => s.GetProductById(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string id, bool _) => _productRepository.GetByIdAsync(id));
        _productService.Setup(s => s.GetProductBySku(It.IsAny<string>()))
            .Returns((string sku) => _productRepository.GetOneAsync(p => p.Sku == sku));

        var securityConfig = new SecurityConfig();
        var guard = new ImportHtmlGuard(new HtmlSanitizationService(securityConfig), securityConfig);
        _import = new ProductVariantImportDataObject(_productService.Object, _attributeService.Object,
            new SeoSettings(), guard, new Mock<ILogger<ProductVariantImportDataObject>>().Object);
    }

    private int Writes => _attributeService.Invocations.Count(i =>
        i.Method.Name.StartsWith("Insert") || i.Method.Name.StartsWith("Update") || i.Method.Name.StartsWith("Delete"));

    private Task<Product> Stored()
    {
        return _productRepository.GetByIdAsync(_product.Id);
    }

    private static ProductVariantsDto ShirtRow(params ProductVariantCombinationDto[] combinations)
    {
        return new ProductVariantsDto {
            ProductSku = "SHIRT",
            Attributes = [
                new ProductVariantAttributeDto {
                    Attribute = "color",
                    Values = [new ProductVariantValueDto { Name = "Red", PriceAdjustment = 1 }, new ProductVariantValueDto { Name = "Blue" }]
                },
                new ProductVariantAttributeDto {
                    Attribute = "Shirt Size",
                    Values = [new ProductVariantValueDto { Name = "M" }, new ProductVariantValueDto { Name = "XL" }]
                }
            ],
            Combinations = combinations.ToList()
        };
    }

    private static ProductVariantCombinationDto Combination(string color, string size, string sku = null,
        double? price = null)
    {
        return new ProductVariantCombinationDto {
            Values = new Dictionary<string, string> { ["color"] = color, ["shirt-size"] = size },
            Sku = sku, Price = price
        };
    }

    private static string ValueId(Product product, ProductAttribute attribute, string name)
    {
        return product.ProductAttributeMappings.Single(m => m.ProductAttributeId == attribute.Id)
            .ProductAttributeValues.Single(v => v.Name == name).Id;
    }

    private static string MappingId(Product product, ProductAttribute attribute)
    {
        return product.ProductAttributeMappings.Single(m => m.ProductAttributeId == attribute.Id).Id;
    }

    [TestMethod]
    public async Task Create_MappingsValuesAndCombination()
    {
        var result = await _import.Import([ShirtRow(Combination("Red", "XL", "SHIRT-RED-XL", 12))], false);

        var row = result.Rows.Single();
        Assert.AreEqual(ImportRowStatus.Updated, row.Status, string.Join(" ", row.Errors));
        Assert.AreEqual(_product.Id, row.Id);
        Assert.AreEqual("SHIRT", row.Key);

        var stored = await Stored();
        Assert.HasCount(2, stored.ProductAttributeMappings);
        var color = stored.ProductAttributeMappings.Single(m => m.ProductAttributeId == _color.Id);
        Assert.AreEqual(AttributeControlType.DropdownList, color.AttributeControlTypeId);
        Assert.IsTrue(color.IsRequired);
        Assert.IsTrue(color.Combination);
        Assert.AreEqual(0, color.DisplayOrder);
        Assert.AreEqual(1, stored.ProductAttributeMappings.Single(m => m.ProductAttributeId == _size.Id).DisplayOrder);
        Assert.AreEqual(1, color.ProductAttributeValues.Single(v => v.Name == "Red").PriceAdjustment);
        Assert.HasCount(2, color.ProductAttributeValues);

        var combination = stored.ProductAttributeCombinations.Single();
        Assert.AreEqual("SHIRT-RED-XL", combination.Sku);
        Assert.AreEqual(12, combination.OverriddenPrice);
        Assert.HasCount(2, combination.Attributes);
        Assert.IsTrue(combination.Attributes.Any(a =>
            a.Key == MappingId(stored, _color) && a.Value == ValueId(stored, _color, "Red")));
        Assert.IsTrue(combination.Attributes.Any(a =>
            a.Key == MappingId(stored, _size) && a.Value == ValueId(stored, _size, "XL")));
    }

    [TestMethod]
    public async Task Resend_IsNoOp_NoDuplicates()
    {
        var row = ShirtRow(Combination("Red", "XL", "SHIRT-RED-XL"), Combination("Blue", "M", "SHIRT-BLUE-M"));
        await _import.Import([row], false);

        var again = await _import.Import([row], false);

        Assert.AreEqual(ImportRowStatus.Updated, again.Rows.Single().Status);
        Assert.IsEmpty(again.Rows.Single().Warnings);
        var stored = await Stored();
        Assert.HasCount(2, stored.ProductAttributeMappings);
        Assert.IsTrue(stored.ProductAttributeMappings.All(m => m.ProductAttributeValues.Count == 2));
        Assert.HasCount(2, stored.ProductAttributeCombinations);
    }

    [TestMethod]
    public async Task Resend_WritesNothing()
    {
        var row = ShirtRow(Combination("Red", "XL", "SHIRT-RED-XL"));
        await _import.Import([row], false);
        var writes = Writes;

        await _import.Import([row], false);

        Assert.AreEqual(writes, Writes);
    }

    [TestMethod]
    public async Task Combination_MatchIsOrderIndependent()
    {
        await _import.Import([ShirtRow(Combination("Red", "XL", "SHIRT-RED-XL"))], false);

        var result = await _import.Import([
            new ProductVariantsDto {
                ProductSku = "SHIRT",
                Combinations = [
                    new ProductVariantCombinationDto {
                        Values = new Dictionary<string, string> { ["Shirt Size"] = "xl", ["Color"] = "RED" }, Price = 15
                    }
                ]
            }
        ], false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows.Single().Status);
        var combination = (await Stored()).ProductAttributeCombinations.Single();
        Assert.AreEqual(15, combination.OverriddenPrice);
    }

    [TestMethod]
    public async Task Combination_PartialUpdate_KeepsSkuGtinMpn()
    {
        var first = ShirtRow(Combination("Red", "XL", "SHIRT-RED-XL", 12));
        first.Combinations[0].Gtin = "123";
        first.Combinations[0].Mpn = "MPN-1";
        await _import.Import([first], false);

        await _import.Import([
            new ProductVariantsDto { ProductSku = "SHIRT", Combinations = [Combination("Red", "XL", price: 20)] }
        ], false);

        var combination = (await Stored()).ProductAttributeCombinations.Single();
        Assert.AreEqual(20, combination.OverriddenPrice);
        Assert.AreEqual("SHIRT-RED-XL", combination.Sku);
        Assert.AreEqual("123", combination.Gtin);
        Assert.AreEqual("MPN-1", combination.Mpn);
    }

    [TestMethod]
    public async Task UnknownProduct_Rejected()
    {
        var result = await _import.Import([
            new ProductVariantsDto { ProductId = "nope", ProductSku = "SHIRT" },
            new ProductVariantsDto { ProductSku = "NO-SUCH-SKU" },
            new ProductVariantsDto()
        ], false);

        Assert.AreEqual(3, result.Rejected);
        Assert.AreEqual("Product 'nope' was not found.", result.Rows[0].Errors.Single());
        Assert.AreEqual("Product with SKU 'NO-SUCH-SKU' was not found.", result.Rows[1].Errors.Single());
        Assert.AreEqual("ProductId or ProductSku is required.", result.Rows[2].Errors.Single());
        Assert.AreEqual(0, Writes);
    }

    [TestMethod]
    public async Task UnknownAttribute_RejectsWholeRow()
    {
        var row = ShirtRow(Combination("Red", "XL"));
        row.Attributes.Add(new ProductVariantAttributeDto { Attribute = "material", Values = [new() { Name = "Wool" }] });

        var result = await _import.Import([row], false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows.Single().Status);
        Assert.Contains("Attribute 'material' was not found.", result.Rows.Single().Errors);
        Assert.AreEqual(0, Writes);
        Assert.IsEmpty((await Stored()).ProductAttributeMappings);
    }

    [TestMethod]
    public async Task UnknownAttributeInCombination_RejectsWholeRow()
    {
        var row = ShirtRow(new ProductVariantCombinationDto {
            Values = new Dictionary<string, string> { ["color"] = "Red", ["material"] = "Wool" }
        });

        var result = await _import.Import([row], false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows.Single().Status);
        Assert.Contains("Attribute 'material' was not found.", result.Rows.Single().Errors);
        Assert.AreEqual(0, Writes);
    }

    [TestMethod]
    public async Task AttributeById_IsMatched()
    {
        var result = await _import.Import([
            new ProductVariantsDto {
                ProductId = _product.Id,
                Attributes = [new ProductVariantAttributeDto { Attribute = _color.Id, Values = [new() { Name = "Red" }] }]
            }
        ], false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows.Single().Status);
        Assert.AreEqual(_product.Id, result.Rows.Single().Key);
        Assert.AreEqual(_color.Id, (await Stored()).ProductAttributeMappings.Single().ProductAttributeId);
    }

    [TestMethod]
    public async Task TextBoxAttributeInCombination_CombinationSkippedWithWarning()
    {
        var row = ShirtRow(Combination("Red", "XL", "OK"),
            new ProductVariantCombinationDto {
                Values = new Dictionary<string, string> { ["color"] = "Blue", ["shirt-size"] = "M" }, Sku = "TB"
            });
        row.Attributes[1].ControlType = AttributeControlType.TextBox;

        var result = await _import.Import([row], false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows.Single().Status);
        Assert.IsTrue(result.Rows.Single().Warnings.Any(w => w.Contains("Shirt Size") && w.Contains("TextBox")),
            string.Join(" ", result.Rows.Single().Warnings));
        Assert.IsEmpty((await Stored()).ProductAttributeCombinations);
    }

    [TestMethod]
    public async Task CombinationNotCoveringEveryMapping_StoredWithWarning()
    {
        var result = await _import.Import([
            ShirtRow(new ProductVariantCombinationDto {
                Values = new Dictionary<string, string> { ["color"] = "Red" }, Sku = "RED-ONLY"
            })
        ], false);

        var row = result.Rows.Single();
        Assert.AreEqual(ImportRowStatus.Updated, row.Status);
        Assert.AreEqual("Combination 1 does not cover 'Shirt Size'; the storefront will not match it.",
            row.Warnings.Single());
        Assert.AreEqual("RED-ONLY", (await Stored()).ProductAttributeCombinations.Single().Sku);
    }

    [TestMethod]
    public async Task EmptyAttributeKeyInCombination_RejectsRow()
    {
        var result = await _import.Import([
            ShirtRow(new ProductVariantCombinationDto {
                Values = new Dictionary<string, string> { [""] = "Red", ["shirt-size"] = "XL" }
            })
        ], false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows.Single().Status);
        Assert.Contains("Combination attribute is required.", result.Rows.Single().Errors);
        Assert.AreEqual(0, Writes);
    }

    [TestMethod]
    public async Task CombinationOffAttributeInCombination_SkippedWithWarning()
    {
        var row = ShirtRow(Combination("Red", "XL", "X"));
        row.Attributes[1].Combination = false;

        var result = await _import.Import([row], false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows.Single().Status);
        Assert.AreEqual(
            "Combination 1 was skipped: attribute 'Shirt Size' does not take part in combinations (Combination is off).",
            result.Rows.Single().Warnings.Single());
        Assert.IsEmpty((await Stored()).ProductAttributeCombinations);
    }

    [TestMethod]
    public async Task HiddenAttributeInCombination_SkippedWithWarning()
    {
        var row = ShirtRow(Combination("Red", "XL", "X"));
        row.Attributes[1].ControlType = AttributeControlType.Hidden;

        var result = await _import.Import([row], false);

        Assert.AreEqual(
            "Combination 1 was skipped: attribute 'Shirt Size' is a Hidden attribute and has no values to combine.",
            result.Rows.Single().Warnings.Single());
        Assert.IsEmpty((await Stored()).ProductAttributeCombinations);
    }

    [TestMethod]
    public async Task MappingTurnedNonSelectable_WithCombinations_Warns()
    {
        await _import.Import([ShirtRow(Combination("Red", "XL", "X"))], false);

        var result = await _import.Import([
            new ProductVariantsDto {
                ProductSku = "SHIRT",
                Attributes = [new ProductVariantAttributeDto { Attribute = "color", ControlType = AttributeControlType.TextBox }]
            }
        ], false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows.Single().Status);
        CollectionAssert.AreEquivalent(new[] {
            "Mapping 'Color' changed from DropdownList to TextBox.",
            "Mapping 'Color' is no longer selectable; its combinations cannot be chosen in the storefront."
        }, result.Rows.Single().Warnings.ToArray());
        Assert.AreEqual(AttributeControlType.TextBox,
            (await Stored()).ProductAttributeMappings.Single(m => m.ProductAttributeId == _color.Id).AttributeControlTypeId);
    }

    [TestMethod]
    public async Task DuplicateCombinationSku_SkippedWithWarning()
    {
        await _import.Import([ShirtRow(Combination("Red", "XL", "DUP"))], false);

        var result = await _import.Import([
            ShirtRow(Combination("Blue", "M", "dup"), Combination("Red", "M", "NEW"), Combination("Blue", "XL", "NEW"))
        ], false);

        var row = result.Rows.Single();
        Assert.AreEqual(ImportRowStatus.Updated, row.Status);
        Assert.HasCount(2, row.Warnings, string.Join(" ", row.Warnings));
        Assert.IsTrue(row.Warnings.All(w => w.Contains("SKU")));
        var stored = await Stored();
        Assert.HasCount(2, stored.ProductAttributeCombinations);
        Assert.AreEqual(1, stored.ProductAttributeCombinations.Count(c => c.Sku == "NEW"));
    }

    [TestMethod]
    public async Task MoreThan50Combinations_Rejected()
    {
        var row = ShirtRow(Enumerable.Range(0, 51).Select(i => Combination("Red", "XL", $"S{i}")).ToArray());

        var result = await _import.Import([row], false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows.Single().Status);
        Assert.AreEqual(0, Writes);
    }

    [TestMethod]
    public async Task ValueUpdate_ChangesOnlyProvidedFields()
    {
        var create = ShirtRow();
        create.Attributes[0].Values[0] = new ProductVariantValueDto {
            Name = "Red", PriceAdjustment = 1, WeightAdjustment = 2, Cost = 3, IsPreSelected = true,
            ColorSquaresRgb = "#ff0000", DisplayOrder = 4
        };
        await _import.Import([create], false);

        await _import.Import([
            new ProductVariantsDto {
                ProductSku = "SHIRT",
                Attributes = [new ProductVariantAttributeDto { Attribute = "color", Values = [new() { Name = "RED", Cost = 9 }] }]
            }
        ], false);

        var stored = await Stored();
        var color = stored.ProductAttributeMappings.Single(m => m.ProductAttributeId == _color.Id);
        var red = color.ProductAttributeValues.Single(v => v.Name == "Red");
        Assert.AreEqual(9, red.Cost);
        Assert.AreEqual(1, red.PriceAdjustment);
        Assert.AreEqual(2, red.WeightAdjustment);
        Assert.IsTrue(red.IsPreSelected);
        Assert.AreEqual("#ff0000", red.ColorSquaresRgb);
        Assert.AreEqual(4, red.DisplayOrder);
        Assert.HasCount(2, color.ProductAttributeValues);
    }

    private async Task StoreColorSquaresMapping()
    {
        await _import.Import([
            new ProductVariantsDto {
                ProductSku = "SHIRT",
                Attributes = [
                    new ProductVariantAttributeDto {
                        Attribute = "color", ControlType = AttributeControlType.ColorSquares, IsRequired = false,
                        Combination = false, Values = [new() { Name = "Red", ColorSquaresRgb = "#ff0000" }]
                    }
                ]
            }
        ], false);
    }

    [TestMethod]
    public async Task MappingUpdate_OmittedControlTypeRequiredAndCombination_AreKept()
    {
        await StoreColorSquaresMapping();

        var result = await _import.Import([
            new ProductVariantsDto {
                ProductSku = "SHIRT",
                Attributes = [new ProductVariantAttributeDto { Attribute = "color", Values = [new() { Name = "Blue" }] }]
            }
        ], false);

        Assert.IsEmpty(result.Rows.Single().Warnings);
        var color = (await Stored()).ProductAttributeMappings.Single(m => m.ProductAttributeId == _color.Id);
        Assert.AreEqual(AttributeControlType.ColorSquares, color.AttributeControlTypeId);
        Assert.IsFalse(color.IsRequired);
        Assert.IsFalse(color.Combination);
        CollectionAssert.AreEquivalent(new[] { "Red", "Blue" }, color.ProductAttributeValues.Select(v => v.Name).ToArray());
    }

    [TestMethod]
    public async Task MappingUpdate_ExplicitControlTypeChange_Warns()
    {
        await StoreColorSquaresMapping();

        var result = await _import.Import([
            new ProductVariantsDto {
                ProductSku = "SHIRT",
                Attributes = [new ProductVariantAttributeDto { Attribute = "color", ControlType = AttributeControlType.RadioList }]
            }
        ], false);

        Assert.AreEqual("Mapping 'Color' changed from ColorSquares to RadioList.", result.Rows.Single().Warnings.Single());
        Assert.AreEqual(AttributeControlType.RadioList,
            (await Stored()).ProductAttributeMappings.Single(m => m.ProductAttributeId == _color.Id).AttributeControlTypeId);
    }

    [TestMethod]
    public async Task MappingUpdate_ExplicitSameControlType_DoesNotWarn()
    {
        await StoreColorSquaresMapping();

        var result = await _import.Import([
            new ProductVariantsDto {
                ProductSku = "SHIRT",
                Attributes = [new ProductVariantAttributeDto { Attribute = "color", ControlType = AttributeControlType.ColorSquares }]
            }
        ], false);

        Assert.IsEmpty(result.Rows.Single().Warnings);
    }

    [TestMethod]
    public async Task MappingUpdate_ChangesOnlyProvidedFields_KeepsValues()
    {
        await _import.Import([ShirtRow()], false);

        await _import.Import([
            new ProductVariantsDto {
                ProductSku = "SHIRT",
                Attributes = [new ProductVariantAttributeDto { Attribute = "color", IsRequired = false, Values = [new() { Name = "Green" }] }]
            }
        ], false);

        var color = (await Stored()).ProductAttributeMappings.Single(m => m.ProductAttributeId == _color.Id);
        Assert.IsFalse(color.IsRequired);
        Assert.AreEqual(AttributeControlType.DropdownList, color.AttributeControlTypeId);
        Assert.IsTrue(color.Combination);
        CollectionAssert.AreEquivalent(new[] { "Red", "Blue", "Green" },
            color.ProductAttributeValues.Select(v => v.Name).ToArray());
    }

    [TestMethod]
    public async Task NewValueOnExistingMapping_IsInsertedAndCombinable()
    {
        await _import.Import([ShirtRow()], false);

        var result = await _import.Import([
            new ProductVariantsDto {
                ProductSku = "SHIRT",
                Attributes = [new ProductVariantAttributeDto { Attribute = "color", Values = [new() { Name = "Green" }] }],
                Combinations = [Combination("Green", "M", "SHIRT-GREEN-M")]
            }
        ], false);

        Assert.IsEmpty(result.Rows.Single().Warnings);
        Assert.AreEqual(1, _attributeService.Invocations.Count(i => i.Method.Name == "InsertProductAttributeValue"));
        var stored = await Stored();
        Assert.HasCount(3, stored.ProductAttributeMappings.Single(m => m.ProductAttributeId == _color.Id)
            .ProductAttributeValues);
        Assert.IsTrue(stored.ProductAttributeCombinations.Single().Attributes
            .Any(a => a.Value == ValueId(stored, _color, "Green")));
    }

    [TestMethod]
    public async Task DryRun_WritesNothing()
    {
        var result = await _import.Import([ShirtRow(Combination("Red", "XL", "SHIRT-RED-XL"))], true);

        Assert.IsTrue(result.DryRun);
        Assert.AreEqual(ImportRowStatus.Updated, result.Rows.Single().Status);
        Assert.AreEqual(0, Writes);
        var stored = await Stored();
        Assert.IsEmpty(stored.ProductAttributeMappings);
        Assert.IsEmpty(stored.ProductAttributeCombinations);
    }

    [TestMethod]
    public async Task DryRun_OnExistingVariants_WritesNothing()
    {
        await _import.Import([ShirtRow(Combination("Red", "XL", "SHIRT-RED-XL"))], false);
        var writes = Writes;
        var row = ShirtRow(Combination("Red", "XL", "CHANGED", 99), Combination("Blue", "M", "NEW"));
        row.Attributes[0].Values.Add(new ProductVariantValueDto { Name = "Green" });
        row.Attributes[0].IsRequired = false;

        var result = await _import.Import([row], true);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows.Single().Status);
        Assert.AreEqual(writes, Writes);
        var stored = await Stored();
        Assert.HasCount(1, stored.ProductAttributeCombinations);
        Assert.AreEqual("SHIRT-RED-XL", stored.ProductAttributeCombinations.Single().Sku);
        Assert.HasCount(2, stored.ProductAttributeMappings.Single(m => m.ProductAttributeId == _color.Id)
            .ProductAttributeValues);
    }

    [TestMethod]
    public async Task CancelledBeforeSecondRow_SecondRowNotProcessed()
    {
        using var cts = new CancellationTokenSource();
        _attributeService.Setup(s => s.InsertProductAttributeMapping(It.IsAny<ProductAttributeMapping>(), It.IsAny<string>()))
            .Callback(() => cts.Cancel()).CallBase();

        var result = await _import.Import([
            ShirtRow(),
            new ProductVariantsDto { ProductSku = "SHIRT", Combinations = [Combination("Red", "XL", "X")] }
        ], false, cts.Token);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[1].Status);
        Assert.AreEqual(ImportRowResult.NotProcessedError, result.Rows[1].Errors.Single());
        Assert.AreEqual("SHIRT", result.Rows[1].Key);
        Assert.IsEmpty((await Stored()).ProductAttributeCombinations);
    }

    [TestMethod]
    public async Task MarkupInValueName_Rejected()
    {
        var row = ShirtRow();
        row.Attributes[0].Values.Add(new ProductVariantValueDto { Name = "<img src=x onerror=alert(1)>" });

        var result = await _import.Import([row], false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows.Single().Status);
        Assert.AreEqual(0, Writes);
    }

    [TestMethod]
    public async Task StorefrontLookup_FindsCreatedCombination()
    {
        await _import.Import([ShirtRow(Combination("Red", "XL", "SHIRT-RED-XL", 12))], false);
        var stored = await Stored();

        //the storefront builds the selection mapping by mapping, in display order, as the attribute parser does
        IList<CustomAttribute> selection = [];
        selection = ProductExtensions.AddProductAttribute(selection,
            stored.ProductAttributeMappings.Single(m => m.ProductAttributeId == _size.Id), ValueId(stored, _size, "XL"));
        selection = ProductExtensions.AddProductAttribute(selection,
            stored.ProductAttributeMappings.Single(m => m.ProductAttributeId == _color.Id), ValueId(stored, _color, "Red"));

        var found = stored.FindProductAttributeCombination(selection);

        Assert.IsNotNull(found);
        Assert.AreEqual("SHIRT-RED-XL", found.Sku);
        Assert.AreEqual("SHIRT-RED-XL", stored.FormatSku(selection));
        Assert.HasCount(2, stored.ParseProductAttributeValues(found.Attributes));
    }
}
