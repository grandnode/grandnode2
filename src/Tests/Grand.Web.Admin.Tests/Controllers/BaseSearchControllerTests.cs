using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Domain;
using Grand.Domain.Admin;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseSearchControllerTests
{
    // Concrete subclass exists only to instantiate the abstract base for direct-call unit tests,
    // with PickerStoreId parameterized so both the Admin/Vendor shape ("") and the Store shape (a
    // real store id) can be exercised without needing the 3 real host subclasses - same pattern as
    // BaseTaxCategoryControllerTests.
    private class TestSearchController(
        ICategoryService categoryService,
        IBrandService brandService,
        ICollectionService collectionService,
        AdminSearchSettings adminSearchSettings,
        string pickerStoreId)
        : BaseSearchController(categoryService, brandService, collectionService, adminSearchSettings)
    {
        protected override string PickerStoreId => pickerStoreId;
    }

    private Mock<ICategoryService> _categoryServiceMock = null!;
    private Mock<IBrandService> _brandServiceMock = null!;
    private Mock<ICollectionService> _collectionServiceMock = null!;
    private AdminSearchSettings _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _brandServiceMock = new Mock<IBrandService>();
        _collectionServiceMock = new Mock<ICollectionService>();
        _settings = new AdminSearchSettings {
            CategorySizeLimit = 10,
            CollectionSizeLimit = 10,
            BrandSizeLimit = 10
        };
    }

    private TestSearchController CreateController(string pickerStoreId) =>
        new(_categoryServiceMock.Object, _brandServiceMock.Object, _collectionServiceMock.Object, _settings, pickerStoreId);

    private static DataSourceRequestFilter EmptyFilter() => new() { Filters = [] };

    // Anonymous JSON payloads aren't used here (DataSourceResult is a public type with public
    // members), but Data's items are instances of a private nested record - read them via
    // reflection, same pattern established in BasePictureControllerTests.
    private static (string Id, string Name)[] ReadRows(IActionResult result)
    {
        var data = (System.Collections.IEnumerable)((JsonResult)result).Value!.GetType().GetProperty("Data")!
            .GetValue(((JsonResult)result).Value)!;
        var rows = new List<(string, string)>();
        foreach (var item in data)
        {
            var id = (string)item.GetType().GetProperty("Id")!.GetValue(item)!;
            var name = (string)item.GetType().GetProperty("Name")!.GetValue(item)!;
            rows.Add((id, name));
        }
        return rows.ToArray();
    }

    [TestMethod]
    public async Task Category_DefaultPickerStoreId_PassesEmptyStoreIdToService()
    {
        var controller = CreateController("");
        _categoryServiceMock
            .Setup(s => s.GetAllCategories(null, null, "", 0, 10, false))
            .ReturnsAsync(new PagedList<Category>([new Category { Id = "cat-1", Name = "Books" }], 0, 10));
        _categoryServiceMock.Setup(s => s.GetFormattedBreadCrumb(It.IsAny<Category>(), ">>", ""))
            .ReturnsAsync("Books");

        var result = await controller.Category(null, EmptyFilter());

        var rows = ReadRows(result);
        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("cat-1", rows[0].Id);
        _categoryServiceMock.Verify(s => s.GetAllCategories(null, null, "", 0, 10, false), Times.Once);
    }

    [TestMethod]
    public async Task Category_OverriddenPickerStoreId_PassesStoreIdToService()
    {
        var controller = CreateController("store-1");
        _categoryServiceMock
            .Setup(s => s.GetAllCategories(null, null, "store-1", 0, 10, false))
            .ReturnsAsync(new PagedList<Category>([], 0, 10));

        await controller.Category(null, EmptyFilter());

        _categoryServiceMock.Verify(s => s.GetAllCategories(null, null, "store-1", 0, 10, false), Times.Once);
    }

    [TestMethod]
    public async Task Collection_DefaultPickerStoreId_PassesEmptyStoreIdToService()
    {
        var controller = CreateController("");
        _collectionServiceMock
            .Setup(s => s.GetAllCollections(null, "", 0, 10, false))
            .ReturnsAsync(new PagedList<Collection>([new Collection { Id = "col-1", Name = "Summer" }], 0, 10));

        var result = await controller.Collection(null, EmptyFilter());

        var rows = ReadRows(result);
        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("Summer", rows[0].Name);
        _collectionServiceMock.Verify(s => s.GetAllCollections(null, "", 0, 10, false), Times.Once);
    }

    [TestMethod]
    public async Task Collection_OverriddenPickerStoreId_PassesStoreIdToService()
    {
        var controller = CreateController("store-1");
        _collectionServiceMock
            .Setup(s => s.GetAllCollections(null, "store-1", 0, 10, false))
            .ReturnsAsync(new PagedList<Collection>([], 0, 10));

        await controller.Collection(null, EmptyFilter());

        _collectionServiceMock.Verify(s => s.GetAllCollections(null, "store-1", 0, 10, false), Times.Once);
    }

    [TestMethod]
    public async Task Brand_DefaultPickerStoreId_PassesEmptyStoreIdToService()
    {
        var controller = CreateController("");
        _brandServiceMock
            .Setup(s => s.GetAllBrands(null, "", 0, 10, false))
            .ReturnsAsync(new PagedList<Brand>([new Brand { Id = "brand-1", Name = "Acme" }], 0, 10));

        var result = await controller.Brand(null, EmptyFilter());

        var rows = ReadRows(result);
        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("Acme", rows[0].Name);
        _brandServiceMock.Verify(s => s.GetAllBrands(null, "", 0, 10, false), Times.Once);
    }

    [TestMethod]
    public async Task Brand_OverriddenPickerStoreId_PassesStoreIdToService()
    {
        var controller = CreateController("store-1");
        _brandServiceMock
            .Setup(s => s.GetAllBrands(null, "store-1", 0, 10, false))
            .ReturnsAsync(new PagedList<Brand>([], 0, 10));

        await controller.Brand(null, EmptyFilter());

        _brandServiceMock.Verify(s => s.GetAllBrands(null, "store-1", 0, 10, false), Times.Once);
    }
}
