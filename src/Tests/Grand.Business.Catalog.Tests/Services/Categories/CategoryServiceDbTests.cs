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
using Grand.Mediator;
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

    [TestInitialize]
    public void InitializeTests()
    {
        _categoryRepository = new MongoDBRepositoryTest<Category>();
        _mediatorMock = new Mock<IMediator>();
        _aclServiceMock = new AclService(new AccessControlConfig());
        _settings = new CatalogSettings();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _categoryService = new CategoryService(_cacheBase, _categoryRepository,
            _mediatorMock.Object, _aclServiceMock, new AccessControlConfig());
    }

    [TestMethod]
    public async Task GetMenuCategories_UsesGivenCustomerAndStore()
    {
        //Arrange
        await _categoryRepository.InsertAsync(new Category { Id = "open", Published = true, IncludeInMenu = true });
        await _categoryRepository.InsertAsync(new Category {
            Id = "vip", Published = true, IncludeInMenu = true,
            LimitedToGroups = true, CustomerGroups = new List<string> { "vip" }
        });
        await _categoryRepository.InsertAsync(new Category {
            Id = "s2", Published = true, IncludeInMenu = true,
            LimitedToStores = true, Stores = new List<string> { "store2" }
        });
        var vipCustomer = new Customer();
        vipCustomer.Groups.Add("vip");

        //Act
        var guestStore1 = await _categoryService.GetMenuCategories(new Customer(), "store1");
        var vipStore2 = await _categoryService.GetMenuCategories(vipCustomer, "store2");

        //Assert
        CollectionAssert.AreEquivalent(new[] { "open" }, guestStore1.Select(c => c.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "open", "vip", "s2" }, vipStore2.Select(c => c.Id).ToArray());
    }

    [TestMethod]
    public async Task GetCategoryBreadCrumb_StopsAtCategoryHiddenFromGivenCustomer()
    {
        //Arrange
        var parent = new Category {
            Id = "p", Published = true, LimitedToGroups = true, CustomerGroups = new List<string> { "vip" }
        };
        var child = new Category { Id = "c", Published = true, ParentCategoryId = "p" };
        await _categoryRepository.InsertAsync(parent);
        await _categoryRepository.InsertAsync(child);
        var vipCustomer = new Customer();
        vipCustomer.Groups.Add("vip");

        //Act
        var guest = await _categoryService.GetCategoryBreadCrumb(child, new Customer(), "");
        var vip = await _categoryService.GetCategoryBreadCrumb(child, vipCustomer, "");

        //Assert
        Assert.HasCount(1, guest);
        Assert.HasCount(2, vip);
    }

    [TestMethod]
    public async Task InsertCategory()
    {
        //Act
        await _categoryService.InsertCategory(new Category());
        //Assert
        Assert.IsNotEmpty(_categoryRepository.Table);
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
    public async Task DeleteCategory_DetachesItsChildCategories()
    {
        //Arrange
        var parent = new Category { Id = "p", Published = true };
        await _categoryService.InsertCategory(parent);
        await _categoryService.InsertCategory(new Category { Id = "c1", ParentCategoryId = "p" });
        await _categoryService.InsertCategory(new Category { Id = "c2", ParentCategoryId = "p", Published = true });
        await _categoryService.InsertCategory(new Category { Id = "other", ParentCategoryId = "x" });
        //Act
        await _categoryService.DeleteCategory(parent);
        //Assert
        Assert.AreEqual("", _categoryRepository.Table.First(x => x.Id == "c1").ParentCategoryId);
        Assert.AreEqual("", _categoryRepository.Table.First(x => x.Id == "c2").ParentCategoryId);
        Assert.AreEqual("x", _categoryRepository.Table.First(x => x.Id == "other").ParentCategoryId);
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
        Assert.HasCount(4, _categoryRepository.Table);
        Assert.IsNull(_categoryRepository.Table.FirstOrDefault(x => x.Id == "1"));
    }

    [TestMethod]
    public async Task GetCategoryBreadCrumb()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var category = new Category { Id = "6", ParentCategoryId = "3", Published = true };
        await _categoryService.InsertCategory(category);
        var result = await _categoryService.GetCategoryBreadCrumb(category, new Customer(), "");
        Assert.HasCount(2, result);
        Assert.IsTrue(result.Any(c => c.Id.Equals("6")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("3")));
    }

    [TestMethod]
    public void GetCategoryBreadCrumb_AllCategory()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var category = new Category { ParentCategoryId = "3" };
        var result = _categoryService.GetCategoryBreadCrumb(category, allCategory, new Customer(), "");
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetAllCategories()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategories(parentId: null, categoryName: "", storeId: "", customer: new Customer());
        Assert.HasCount(5, result);
    }

    [TestMethod]
    public async Task GetMenuCategories()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetMenuCategories(new Customer(), "");
        Assert.HasCount(1, result);
    }

    [TestMethod]
    public async Task GetAllCategoriesByParentCategoryId()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategoriesByParentCategoryId(new Customer(), "", "5");
        Assert.HasCount(1, result);
    }

    [TestMethod]
    public async Task GetAllCategoriesDisplayedOnHomePage()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategoriesDisplayedOnHomePage(new Customer(), "");
        Assert.HasCount(2, result);
    }

    [TestMethod]
    public async Task GetAllCategoriesFeaturedProductsOnHomePage()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategoriesFeaturedProductsOnHomePage(new Customer(), "");
        Assert.HasCount(2, result);
    }

    [TestMethod]
    public async Task GetAllCategoriesSearchBox()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var result = await _categoryService.GetAllCategoriesSearchBox(new Customer(), "");
        Assert.HasCount(2, result);
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
        var result = _categoryService.GetCategoryBreadCrumb(category, allCategory, new Customer(), "");
        Assert.HasCount(2, result);
        Assert.IsTrue(result.Any(c => c.Id.Equals("6")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("3")));
    }

    [TestMethod]
    public void GetCategoryBreadCrumb_ShouldReturnThreeElement()
    {
        var allCategory = GetMockCategoryList();
        allCategory.ToList().ForEach(x => _categoryService.InsertCategory(x).GetAwaiter().GetResult());
        var category = new Category { Id = "6", ParentCategoryId = "1", Published = true };
        var result = _categoryService.GetCategoryBreadCrumb(category, allCategory, new Customer(), "");
        Assert.HasCount(3, result);
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