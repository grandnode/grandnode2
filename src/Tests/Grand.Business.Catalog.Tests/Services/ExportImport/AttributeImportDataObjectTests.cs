using Grand.Business.Catalog.Services.ExportImport;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.ExportImport;

[TestClass]
public class AttributeImportDataObjectTests
{
    private Mock<IProductAttributeService> _productAttributeService;
    private ProductAttributeImportDataObject _productImport;
    private List<ProductAttribute> _productAttributes;
    private Mock<ISpecificationAttributeService> _specificationAttributeService;
    private SpecificationAttributeImportDataObject _specificationImport;
    private List<SpecificationAttribute> _specificationAttributes;
    private int _writes;

    [TestInitialize]
    public void Init()
    {
        _writes = 0;
        _specificationAttributes = [];
        _productAttributes = [];
        var securityConfig = new SecurityConfig();
        var guard = new ImportHtmlGuard(new HtmlSanitizationService(securityConfig), securityConfig);

        _specificationAttributeService = new Mock<ISpecificationAttributeService>();
        _specificationAttributeService.Setup(s => s.GetSpecificationAttributeById(It.IsAny<string>()))
            .ReturnsAsync((string id) => _specificationAttributes.FirstOrDefault(a => a.Id == id));
        _specificationAttributeService.Setup(s => s.GetSpecificationAttributeBySeName(It.IsAny<string>()))
            .ReturnsAsync((string s) => _specificationAttributes.FirstOrDefault(a => a.SeName == s));
        _specificationAttributeService.Setup(s => s.InsertSpecificationAttribute(It.IsAny<SpecificationAttribute>()))
            .Callback((SpecificationAttribute a) => { _writes++; _specificationAttributes.Add(a); })
            .Returns(Task.CompletedTask);
        _specificationAttributeService.Setup(s => s.UpdateSpecificationAttribute(It.IsAny<SpecificationAttribute>()))
            .Callback(() => _writes++).Returns(Task.CompletedTask);
        _specificationImport =
            new SpecificationAttributeImportDataObject(_specificationAttributeService.Object, new SeoSettings(), guard,
                new Mock<ILogger<SpecificationAttributeImportDataObject>>().Object);

        _productAttributeService = new Mock<IProductAttributeService>();
        _productAttributeService.Setup(s => s.GetProductAttributeById(It.IsAny<string>()))
            .ReturnsAsync((string id) => _productAttributes.FirstOrDefault(a => a.Id == id));
        _productAttributeService.Setup(s => s.GetAllProductAttributes(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(() => new Grand.Domain.PagedList<ProductAttribute>(_productAttributes, 0, int.MaxValue));
        _productAttributeService.Setup(s => s.InsertProductAttribute(It.IsAny<ProductAttribute>()))
            .Callback((ProductAttribute a) => { _writes++; _productAttributes.Add(a); }).Returns(Task.CompletedTask);
        _productAttributeService.Setup(s => s.UpdateProductAttribute(It.IsAny<ProductAttribute>()))
            .Callback(() => _writes++).Returns(Task.CompletedTask);
        _productImport = new ProductAttributeImportDataObject(_productAttributeService.Object, new SeoSettings(), guard,
            new Mock<ILogger<ProductAttributeImportDataObject>>().Object);
    }

    [TestMethod]
    public async Task Specification_Create_WithOptions()
    {
        var result = await _specificationImport.Import([
            new SpecificationAttributeDto {
                Name = "Color Family", DisplayOrder = 3,
                Options = [new() { Name = "Red", ColorSquaresRgb = "#ff0000" }, new() { Name = "Blue", DisplayOrder = 2 }]
            }
        ], false);

        Assert.AreEqual(1, result.Created);
        var created = _specificationAttributes.Single();
        Assert.AreEqual("color-family", created.SeName);
        Assert.AreEqual(3, created.DisplayOrder);
        Assert.AreEqual(2, created.SpecificationAttributeOptions.Count);
        Assert.AreEqual("#ff0000", created.SpecificationAttributeOptions.First(o => o.Name == "Red").ColorSquaresRgb);
        Assert.AreEqual(2, created.SpecificationAttributeOptions.First(o => o.Name == "Blue").DisplayOrder);
    }

    [TestMethod]
    public async Task Specification_UpdateBySeName_AddsOptionAndKeepsOld()
    {
        var existing = new SpecificationAttribute { Id = "1", Name = "Size", SeName = "size" };
        existing.SpecificationAttributeOptions.Add(new SpecificationAttributeOption { Name = "Small", DisplayOrder = 1 });
        existing.SpecificationAttributeOptions.Add(new SpecificationAttributeOption { Name = "Large" });
        _specificationAttributes.Add(existing);

        var result = await _specificationImport.Import([
            new SpecificationAttributeDto {
                SeName = "size", Options = [new() { Name = "SMALL" }, new() { Name = "Medium" }]
            }
        ], false);

        Assert.AreEqual(1, result.Updated);
        Assert.AreEqual("1", result.Rows[0].Id);
        Assert.AreEqual("Size", existing.Name);
        Assert.AreEqual(3, existing.SpecificationAttributeOptions.Count);
        Assert.AreEqual(1, existing.SpecificationAttributeOptions.First(o => o.Name == "Small").DisplayOrder,
            "a field that was not provided is kept");
        Assert.IsTrue(existing.SpecificationAttributeOptions.Any(o => o.Name == "Large"));
        Assert.IsTrue(existing.SpecificationAttributeOptions.Any(o => o.Name == "Medium"));
    }

    [TestMethod]
    public async Task Specification_DuplicateOptionNames_CollapseToOne()
    {
        await _specificationImport.Import([
            new SpecificationAttributeDto {
                Name = "Material",
                Options = [new() { Name = "Wool" }, new() { Name = "wool", DisplayOrder = 5 }]
            }
        ], false);

        var option = _specificationAttributes.Single().SpecificationAttributeOptions.Single();
        Assert.AreEqual("Wool", option.Name);
        Assert.AreEqual(5, option.DisplayOrder);
    }

    [TestMethod]
    public async Task Specification_MarkupInOptionName_Rejected()
    {
        var result = await _specificationImport.Import([
            new SpecificationAttributeDto { Name = "Ok", Options = [new() { Name = "<script>alert(1)</script>" }] }
        ], false);

        Assert.AreEqual(1, result.Rejected);
        Assert.AreEqual(0, _writes);
    }

    [TestMethod]
    public async Task Specification_UnknownId_Rejected_EmptyNameRejected_NewWithoutNameRejected()
    {
        var result = await _specificationImport.Import([
            new SpecificationAttributeDto { Id = "nope", Name = "x" },
            new SpecificationAttributeDto { Name = "" },
            new SpecificationAttributeDto { SeName = "unknown" }
        ], false);

        Assert.AreEqual(3, result.Rejected);
        Assert.AreEqual("Id 'nope' was not found.", result.Rows[0].Errors[0]);
        Assert.AreEqual(0, _writes);
    }

    [TestMethod]
    public async Task Specification_DryRun_WritesNothing()
    {
        var result = await _specificationImport.Import([
            new SpecificationAttributeDto { Name = "Dry", Options = [new() { Name = "A" }] }
        ], true);

        Assert.IsTrue(result.DryRun);
        Assert.AreEqual(1, result.Created);
        Assert.AreEqual(0, _writes);
        Assert.AreEqual(0, _specificationAttributes.Count);
    }

    [TestMethod]
    public async Task Product_Create_WithValues()
    {
        var result = await _productImport.Import([
            new ProductAttributeDto {
                Name = "Gift Wrap", Description = "<p>Nice</p>",
                Values = [new() { Name = "Gold", PriceAdjustment = 2.5, IsPreSelected = true }]
            }
        ], false);

        Assert.AreEqual(1, result.Created);
        var created = _productAttributes.Single();
        Assert.AreEqual("gift-wrap", created.SeName);
        var value = created.PredefinedProductAttributeValues.Single();
        Assert.AreEqual(2.5, value.PriceAdjustment);
        Assert.IsTrue(value.IsPreSelected);
    }

    [TestMethod]
    public async Task Product_UpdateBySeName_AddsValueAndKeepsOld()
    {
        var existing = new ProductAttribute { Id = "7", Name = "Size", SeName = "size" };
        existing.PredefinedProductAttributeValues.Add(new PredefinedProductAttributeValue { Name = "S", Cost = 4 });
        _productAttributes.Add(existing);

        var result = await _productImport.Import([
            new ProductAttributeDto {
                SeName = "SIZE", Values = [new() { Name = "s", PriceAdjustment = 1 }, new() { Name = "M" }]
            }
        ], false);

        Assert.AreEqual(1, result.Updated);
        Assert.AreEqual(2, existing.PredefinedProductAttributeValues.Count);
        var s = existing.PredefinedProductAttributeValues.First(v => v.Name == "S");
        Assert.AreEqual(1, s.PriceAdjustment);
        Assert.AreEqual(4, s.Cost, "a field that was not provided is kept");
    }

    [TestMethod]
    public async Task Product_DuplicateValueNames_CollapseToOne()
    {
        await _productImport.Import([
            new ProductAttributeDto { Name = "A", Values = [new() { Name = "x" }, new() { Name = "X", Cost = 1 }] }
        ], false);

        var value = _productAttributes.Single().PredefinedProductAttributeValues.Single();
        Assert.AreEqual(1, value.Cost);
    }

    [TestMethod]
    public async Task Product_MarkupInValueName_Rejected()
    {
        var result = await _productImport.Import([
            new ProductAttributeDto { Name = "A", Values = [new() { Name = "<img src=x onerror=alert(1)>" }] }
        ], false);

        Assert.AreEqual(1, result.Rejected);
        Assert.AreEqual(0, _writes);
    }

    [TestMethod]
    public async Task Product_UnknownId_Rejected()
    {
        var result = await _productImport.Import([new ProductAttributeDto { Id = "zz", Name = "x" }], false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.AreEqual("Id 'zz' was not found.", result.Rows[0].Errors[0]);
    }

    [TestMethod]
    public async Task Product_DryRun_WritesNothing()
    {
        var result = await _productImport.Import([
            new ProductAttributeDto { Name = "Dry", Values = [new() { Name = "A" }] }
        ], true);

        Assert.AreEqual(1, result.Created);
        Assert.AreEqual(0, _writes);
        Assert.AreEqual(0, _productAttributes.Count);
    }

    private const string NotProcessed = "Not processed: the batch time limit was reached; send these rows again.";

    [TestMethod]
    public async Task Specification_CancelledAfterFirstRow_RestAreRejectedAsNotProcessed()
    {
        using var cts = new CancellationTokenSource();
        _specificationAttributeService.Setup(s => s.InsertSpecificationAttribute(It.IsAny<SpecificationAttribute>()))
            .Callback((SpecificationAttribute a) => { _writes++; _specificationAttributes.Add(a); cts.Cancel(); })
            .Returns(Task.CompletedTask);

        var result = await _specificationImport.Import([
            new SpecificationAttributeDto { Name = "One" }, new SpecificationAttributeDto { Name = "Two" },
            new SpecificationAttributeDto { Name = "Three" }
        ], false, cts.Token);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[1].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[2].Status);
        Assert.AreEqual("Three", result.Rows[2].Key);
        Assert.AreEqual(NotProcessed, result.Rows[1].Errors.Single());
        Assert.AreEqual(1, _writes);
    }

    [TestMethod]
    public async Task Product_CancelledAfterFirstRow_RestAreRejectedAsNotProcessed()
    {
        using var cts = new CancellationTokenSource();
        _productAttributeService.Setup(s => s.InsertProductAttribute(It.IsAny<ProductAttribute>()))
            .Callback((ProductAttribute a) => { _writes++; _productAttributes.Add(a); cts.Cancel(); })
            .Returns(Task.CompletedTask);

        var result = await _productImport.Import([
            new ProductAttributeDto { Name = "One" }, new ProductAttributeDto { Name = "Two" },
            new ProductAttributeDto { Name = "Three" }
        ], false, cts.Token);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[1].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[2].Status);
        Assert.AreEqual(NotProcessed, result.Rows[2].Errors.Single());
        Assert.AreEqual(1, _writes);
    }

    [TestMethod]
    public async Task Specification_SeNameIsNormalizedBeforeMatching()
    {
        _specificationAttributes.Add(new SpecificationAttribute { Id = "1", Name = "Shirt Size", SeName = "shirt-size" });

        var result = await _specificationImport.Import([new SpecificationAttributeDto { SeName = "Shirt Size" }], false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        Assert.AreEqual("1", result.Rows[0].Id);
        Assert.AreEqual(1, _specificationAttributes.Count);
    }

    [TestMethod]
    public async Task Product_SeNameIsNormalizedBeforeMatching()
    {
        _productAttributes.Add(new ProductAttribute { Id = "7", Name = "Shirt Size", SeName = "shirt-size" });

        var result = await _productImport.Import([new ProductAttributeDto { SeName = "Shirt Size" }], false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        Assert.AreEqual("7", result.Rows[0].Id);
        Assert.AreEqual(1, _productAttributes.Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Specification_NameOnlyResent_MatchesExistingByName(bool dryRun)
    {
        _specificationAttributes.Add(new SpecificationAttribute { Id = "1", Name = "Shirt Size", SeName = "shirt-size" });

        var result = await _specificationImport.Import(
            [new SpecificationAttributeDto { Name = "shirt size", Options = [new() { Name = "XL" }] }], dryRun);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        Assert.AreEqual("1", result.Rows[0].Id);
        Assert.Contains("Matched existing 'Shirt Size' by name.", result.Rows[0].Warnings);
        Assert.AreEqual(1, _specificationAttributes.Count);
        Assert.AreEqual(dryRun ? 0 : 1, _specificationAttributes.Single().SpecificationAttributeOptions.Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Product_NameOnlyResent_MatchesExistingByName(bool dryRun)
    {
        _productAttributes.Add(new ProductAttribute { Id = "7", Name = "Gift Wrap", SeName = "gift-wrap" });

        var result = await _productImport.Import(
            [new ProductAttributeDto { Name = "GIFT WRAP", Values = [new() { Name = "Gold" }] }], dryRun);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        Assert.AreEqual("7", result.Rows[0].Id);
        Assert.Contains("Matched existing 'Gift Wrap' by name.", result.Rows[0].Warnings);
        Assert.AreEqual(1, _productAttributes.Count);
        Assert.AreEqual(dryRun ? 0 : 1, _productAttributes.Single().PredefinedProductAttributeValues.Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Specification_SlugOfDifferentlyNamedAttribute_IsRejectedAsSeNameTaken(bool dryRun)
    {
        _specificationAttributes.Add(new SpecificationAttribute { Id = "1", Name = "Shirt-Size", SeName = "shirt-size" });

        var result = await _specificationImport.Import([new SpecificationAttributeDto { Name = "Shirt Size" }], dryRun);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.Contains("SeName 'shirt-size' is already used by 'Shirt-Size'.", result.Rows[0].Errors);
        Assert.AreEqual(1, _specificationAttributes.Count);
    }

    [TestMethod]
    public async Task Specification_UpdateByIdToAnotherAttributesSeName_IsRejected()
    {
        _specificationAttributes.Add(new SpecificationAttribute { Id = "1", Name = "Color", SeName = "color" });
        _specificationAttributes.Add(new SpecificationAttribute { Id = "2", Name = "Size", SeName = "size" });

        var result = await _specificationImport.Import([new SpecificationAttributeDto { Id = "2", SeName = "Color" }],
            false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.AreEqual("size", _specificationAttributes[1].SeName);
        Assert.AreEqual(0, _writes);
    }

    [TestMethod]
    public async Task Product_SlugOfDifferentlyNamedAttribute_IsRejectedAsSeNameTaken()
    {
        _productAttributes.Add(new ProductAttribute { Id = "7", Name = "Gift-Wrap", SeName = "gift-wrap" });

        var result = await _productImport.Import([new ProductAttributeDto { Name = "Gift Wrap" }], false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.Contains("SeName 'gift-wrap' is already used by 'Gift-Wrap'.", result.Rows[0].Errors);
        Assert.AreEqual(1, _productAttributes.Count);
    }

    [TestMethod]
    public async Task Product_AttributesAreLoadedOncePerBatch()
    {
        var result = await _productImport.Import([
            new ProductAttributeDto { Name = "Gift Wrap" }, new ProductAttributeDto { Name = "gift wrap" },
            new ProductAttributeDto { SeName = "gift-wrap" }
        ], false);

        Assert.AreEqual(1, result.Created);
        Assert.AreEqual(2, result.Updated);
        _productAttributeService.Verify(
            s => s.GetAllProductAttributes(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [TestMethod]
    public async Task Specification_RowThatThrows_IsRejectedAndBatchContinues()
    {
        _specificationAttributeService.Setup(s => s.InsertSpecificationAttribute(
                It.Is<SpecificationAttribute>(a => a.Name == "Broken")))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _specificationImport.Import([
            new SpecificationAttributeDto { Name = "Broken" }, new SpecificationAttributeDto { Name = "Material" }
        ], false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.Contains(ImportRowResult.FailedError, result.Rows[0].Errors);
        Assert.AreEqual(ImportRowStatus.Created, result.Rows[1].Status);
    }
}
