using System.Linq;
using Grand.Domain.Admin;
using Grand.Domain.Customers;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class SearchControllerAttributeTests
{
    [TestMethod]
    public void IsSubclassOfBaseSearchController()
    {
        Assert.IsTrue(typeof(BaseSearchController).IsAssignableFrom(typeof(SearchController)));
        Assert.AreEqual(typeof(BaseSearchController), typeof(SearchController).BaseType);
    }

    [TestMethod]
    public void HasAuthorizeStoreAttribute()
    {
        var attr = typeof(SearchController).GetCustomAttributes(typeof(AuthorizeStoreAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaStoreAttribute()
    {
        var attr = typeof(SearchController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Store", attr.RouteValue);
    }

    [TestMethod]
    public void HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attr = typeof(SearchController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(SearchController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    // Store is the one host whose PickerStoreId override actually matters (Admin/Vendor keep the
    // base's "" default) - regression guard for the storeId source itself, not just wiring.
    [TestMethod]
    public async System.Threading.Tasks.Task PickerStoreId_ReflectsCurrentCustomerStaffStoreId()
    {
        var categoryServiceMock = new Mock<ICategoryService>();
        categoryServiceMock
            .Setup(s => s.GetAllCategories(null, null, "store-42", 0, It.IsAny<int>(), false))
            .ReturnsAsync(new Grand.Domain.PagedList<Grand.Domain.Catalog.Category>([], 0, 10));

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = "store-42" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var controller = new SearchController(
            categoryServiceMock.Object,
            new Mock<IBrandService>().Object,
            new Mock<ICollectionService>().Object,
            new AdminSearchSettings { CategorySizeLimit = 10 },
            contextAccessorMock.Object);

        await controller.Category(null, new Grand.Web.Common.DataSource.DataSourceRequestFilter { Filters = [] });

        categoryServiceMock.Verify(s => s.GetAllCategories(null, null, "store-42", 0, 10, false), Times.Once);
    }
}
