using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Security;
using Grand.Infrastructure;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Filters;

[TestClass]
public class AuthorizeAdminAttributeTests
{
    private Customer _customer;
    private Mock<IContextAccessor> _contextAccessor;
    private Mock<IGroupService> _groupService;
    private Mock<IRequestPermissionService> _permissionService;

    public AuthorizeAdminAttributeTests()
    {
        DataSettingsManager.Initialize(Path.Join(Path.GetTempPath(), "grand-admin-filter-tests-settings.json"));
        DataSettingsManager.Instance.LoadDataSettings(new DataSettings { ConnectionString = "mongodb://localhost" });
        _customer = new Customer();
        _contextAccessor = new Mock<IContextAccessor>();
        _contextAccessor.Setup(x => x.WorkContext.CurrentCustomer).Returns(() => _customer);
        _permissionService = new Mock<IRequestPermissionService>();
        _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel)).ReturnsAsync(true);
        _groupService = new Mock<IGroupService>();
    }

    private async Task<AuthorizationFilterContext> Run()
    {
        var attribute = new AuthorizeAdminAttribute();
        var filter = (IAsyncAuthorizationFilter)ActivatorUtilities.CreateInstance(
            new ServiceCollection()
                .AddSingleton(_permissionService.Object)
                .AddSingleton(_contextAccessor.Object)
                .AddSingleton(_groupService.Object)
                .AddSingleton(new SecuritySettings())
                .BuildServiceProvider(),
            attribute.ImplementationType, false);
        var context = new AuthorizationFilterContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), []);
        context.Filters.Add((IFilterMetadata)filter);
        await filter.OnAuthorizationAsync(context);
        return context;
    }

    [TestMethod]
    public async Task Staff_IsAdmitted()
    {
        var context = await Run();

        Assert.IsNull(context.Result);
    }

    [TestMethod]
    public async Task SalesManager_WithSalesEmployee_IsAdmitted()
    {
        _customer.SeId = "se1";
        _groupService.Setup(x => x.IsSalesManager(_customer)).ReturnsAsync(true);

        var context = await Run();

        Assert.IsNull(context.Result);
    }

    [TestMethod]
    public async Task SalesManager_WithoutSalesEmployee_IsSentToLogin()
    {
        _groupService.Setup(x => x.IsSalesManager(_customer)).ReturnsAsync(true);

        var context = await Run();

        Assert.AreEqual("AdminLogin", (context.Result as RedirectToRouteResult)?.RouteName);
    }

    [TestMethod]
    public async Task NoPanelPermission_IsSentToLogin()
    {
        _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel)).ReturnsAsync(false);

        var context = await Run();

        Assert.AreEqual("AdminLogin", (context.Result as RedirectToRouteResult)?.RouteName);
    }
}
