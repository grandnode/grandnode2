using Grand.Business.Catalog.Services.Categories;
using Grand.Business.Common.Services.Security;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Categories;

[TestClass]
public class CategoryServiceDbTests
{
    private IAclService _aclServiceMock;
    private MemoryCacheBase _cacheBase;
    private IRepository<Category> _categoryRepository;
    private CategoryService _categoryService;
    private Mock<IMediator> _mediatorMock;
    private CatalogSettings _settings;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void InitializeTests()
    {
        CommonPath.BaseDirectory = "";

        _categoryRepository = new MongoDBRepositoryTest<Category>();
        _workContextMock = new Mock<IWorkContext>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
        _mediatorMock = new Mock<IMediator>();
        _aclServiceMock = new AclService(new AccessControlConfig());
        _settings = new CatalogSettings();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _categoryService = new CategoryService(_cacheBase, _categoryRepository, _workContextMock.Object,
            _mediatorMock.Object, _aclServiceMock, new AccessControlConfig());
    }

    [TestMethod]
    public async Task InsertCategory()
    {
        //Act
        await _categoryService.InsertCategory(new Category());
        //Assert
        Assert.IsTrue(_categoryRepository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateCategory()
    {
        //Arrange
        var category = new Category { Name = "test" };
        await _categoryService.InsertCategory(category);
        category.Name = "test2";
        //Act
        await _categoryService.UpdateCategory(category);
        //Assert
        Assert.IsNotNull(_categoryRepository.Table.FirstOrDefault(x => x.Name == "test2"));
    }

    [TestMethod]
    public async Task DeleteCategory()
    {
        //Arrange
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        //Act
        await _categoryService.DeleteCategory(allCategory.FirstOrDefault(x => x.Id == "1"));
        //Assert
        Assert.IsTrue(_categoryRepository.Table.Count() == 4);
        Assert.IsNull(_categoryRepository.Table.FirstOrDefault(x => x.Id == "1"));
    }

    [TestMethod]
    public async Task GetCategoryBreadCrumb()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var category = new Category { Id = "6", ParentCategoryId = "3", Published = true };
        await _categoryService.InsertCategory(category);
        var result = await _categoryService.GetCategoryBreadCrumb(category);
        Assert.IsTrue(result.Count == 2);
        Assert.IsTrue(result.Any(c => c.Id.Equals("6")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("3")));
    }

    [TestMethod]
    public void GetCategoryBreadCrumb_AllCategory()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var category = new Category { ParentCategoryId = "3" };
        var result = _categoryService.GetCategoryBreadCrumb(category, allCategory);
        Assert.IsTrue(result.Count == 0);
    }

    [TestMethod]
    public async Task GetAllCategories()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategories();
        Assert.IsTrue(result.Count == 5);
    }

    [TestMethod]
    public async Task GetMenuCategories()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetMenuCategories();
        Assert.IsTrue(result.Count == 1);
    }

    [TestMethod]
    public async Task GetAllCategoriesByParentCategoryId()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategoriesByParentCategoryId("5");
        Assert.IsTrue(result.Count == 1);
    }

    [TestMethod]
    public async Task GetAllCategoriesDisplayedOnHomePage()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategoriesDisplayedOnHomePage();
        Assert.IsTrue(result.Count == 2);
    }

    [TestMethod]
    public async Task GetAllCategoriesFeaturedProductsOnHomePage()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategoriesFeaturedProductsOnHomePage();
        Assert.IsTrue(result.Count == 2);
    }

    [TestMethod]
    public async Task GetAllCategoriesSearchBox()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategoriesSearchBox();
        Assert.IsTrue(result.Count == 2);
    }

    [TestMethod]
    public async Task GetCategoryById_IsNotNull()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetCategoryById("1");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCategoryById_IsNull()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetCategoryById("10");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetCategoryBreadCrumb_ShouldReturnTwoElement()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var category = new Category { Id = "6", ParentCategoryId = "3", Published = true };
        await _categoryService.InsertCategory(category);
        var result = _categoryService.GetCategoryBreadCrumb(category, allCategory);
        Assert.IsTrue(result.Count == 2);
        Assert.IsTrue(result.Any(c => c.Id.Equals("6")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("3")));
    }

    [TestMethod]
    public void GetCategoryBreadCrumb_ShouldReturnThreeElement()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var category = new Category { Id = "6", ParentCategoryId = "1", Published = true };
        var result = _categoryService.GetCategoryBreadCrumb(category, allCategory);
        Assert.IsTrue(result.Count == 3);
        Assert.IsTrue(result.Any(c => c.Id.Equals("6")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("1")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("5")));
    }

    [TestMethod]
    public async Task GetFormattedBreadCrumb()
    {
        var exprectedString = "cat5 >> cat1 >> cat6";
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var category = new Category { Id = "6", Name = "cat6", ParentCategoryId = "1", Published = true };
        var result = await _categoryService.GetFormattedBreadCrumb(category);
        Assert.IsTrue(exprectedString.Equals(result));
    }

    [TestMethod]
    public void GetFormattedBreadCrumb_ReturnExprectedString()
    {
        var exprectedString = "cat5 >> cat1 >> cat6";
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var category = new Category { Id = "6", Name = "cat6", ParentCategoryId = "1", Published = true };
        var result = _categoryService.GetFormattedBreadCrumb(category, allCategory);
        Assert.IsTrue(exprectedString.Equals(result));
    }

    [TestMethod]
    public void GetFormattedBreadCrumb_ReturnEmptyString()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = _categoryService.GetFormattedBreadCrumb(null, allCategory);
        Assert.IsTrue(string.IsNullOrEmpty(result));
    }


    private IList<Category> GetMockCategoryList()
    {
        return new List<Category> {
            new() {
                Id = "1", Name = "cat1", Published = true, ParentCategoryId = "5", FeaturedProductsOnHomePage = true
            },
            new() { Id = "2", Name = "cat2", Published = true, IncludeInMenu = true },
            new() { Id = "3", Name = "cat3", Published = true, ShowOnHomePage = true, ShowOnSearchBox = true },
            new() { Id = "4", Name = "cat4", Published = true, ShowOnHomePage = true, ShowOnSearchBox = true },
            new() { Id = "5", Name = "cat5", Published = true, FeaturedProductsOnHomePage = true }
        };
    }
}