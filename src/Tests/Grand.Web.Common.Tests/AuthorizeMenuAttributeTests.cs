using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.System.Admin;
using Grand.Data;
using Grand.Domain.Admin;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Common.Tests;

[TestClass]
public class AuthorizeMenuAttributeTests
{
    private Mock<IAdminSiteMapService> _mockAdminSiteMapService;
    private AuthorizationFilterContext _mockFilterContext;
    private Mock<IPermissionService> _mockPermissionService;
    private Mock<IWorkContext> _mockWorkContext;
    private SecurityConfig _securityConfig;

    [TestInitialize]
    public void Setup()
    {
        CommonPath.BaseDirectory = "";
        var settings = DataSettingsManager.LoadSettings();
        _mockPermissionService = new Mock<IPermissionService>();
        _mockAdminSiteMapService = new Mock<IAdminSiteMapService>();
        _mockWorkContext = new Mock<IWorkContext>();
        _securityConfig = new SecurityConfig();
        var filters = new List<IFilterMetadata> {
            new AuthorizeMenuAttribute()
        };
        _mockFilterContext = new AuthorizationFilterContext(GetMockedAuthorizationFilterContext(), filters);
    }

    private AuthorizationFilterContext GetMockedAuthorizationFilterContext()
    {
        // Mocking HttpContext
        var httpContextMock = new Mock<HttpContext>();

        // Mocking RouteData
        var routeData = new RouteData();
        routeData.Values.Add("controller", "SampleController");
        routeData.Values.Add("action", "SampleAction");

        // Mocking ActionDescriptor
        var actionDescriptorMock = new Mock<ActionDescriptor>();
        //actionDescriptorMock.Object.Filters.Add(new AuthorizeMenuAttribute(false));


        var actionContext = new ActionContext(
            httpContextMock.Object,
            routeData,
            actionDescriptorMock.Object
        );

        // Mocking filters for the context (can be empty if not needed for testing)
        var filters = new List<IFilterMetadata> {
            new AuthorizeMenuAttribute()
        };

        var authorizationFilterContext = new AuthorizationFilterContext(actionContext, filters);

        return authorizationFilterContext;
    }

    [TestMethod]
    public async Task TestAuthorizeMenuAttribute_WithoutIgnoreFilter_DatabaseNotInstalled()
    {
        // Arrange
        var attribute = new AuthorizeMenuAttribute();
        var filter = new AuthorizeMenuAttribute.AuthorizeMenuFilter(
            false,
            _mockPermissionService.Object,
            _mockAdminSiteMapService.Object,
            _mockWorkContext.Object,
            _securityConfig
        );

        // Act
        await filter.OnAuthorizationAsync(_mockFilterContext);

        // Assert
        // Assuming RedirectToRouteResult is not set when the database is not installed
        Assert.IsNull(_mockFilterContext.Result);
    }

    [TestMethod]
    public async Task TestAuthorizeMenuAttribute_WithoutIgnoreFilter_AuthorizeAdminMenuDisabled()
    {
        // Arrange
        _securityConfig.AuthorizeAdminMenu = false;
        var attribute = new AuthorizeMenuAttribute();
        var filter = new AuthorizeMenuAttribute.AuthorizeMenuFilter(
            false,
            _mockPermissionService.Object,
            _mockAdminSiteMapService.Object,
            _mockWorkContext.Object,
            _securityConfig
        );

        // Act
        await filter.OnAuthorizationAsync(_mockFilterContext);

        // Assert
        // Assuming RedirectToRouteResult is not set when AuthorizeAdminMenu is disabled
        Assert.IsNull(_mockFilterContext.Result);
    }

    [TestMethod]
    public async Task TestAuthorizeMenuAttribute_WithValidSiteMap_AllPermissions()
    {
        // Arrange
        var menuSiteMap = new AdminSiteMap {
            AllPermissions = true, PermissionNames = new List<string> { "Permission1" }, ActionName = "SampleAction",
            ControllerName = "SampleController"
        };
        _mockAdminSiteMapService.Setup(s => s.GetSiteMap()).ReturnsAsync(new List<AdminSiteMap> { menuSiteMap });
        _mockPermissionService.Setup(s => s.Authorize(It.IsAny<string>(), It.IsAny<Customer>())).ReturnsAsync(false);
        _securityConfig.AuthorizeAdminMenu = true;
        var attribute = new AuthorizeMenuAttribute();
        var filter = new AuthorizeMenuAttribute.AuthorizeMenuFilter(
            false,
            _mockPermissionService.Object,
            _mockAdminSiteMapService.Object,
            _mockWorkContext.Object,
            _securityConfig
        );

        // Act
        await filter.OnAuthorizationAsync(_mockFilterContext);

        // Assert
        Assert.IsInstanceOfType(_mockFilterContext.Result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task TestAuthorizeMenuAttribute_NoPermissionsInSiteMap()
    {
        // Arrange
        var menuSiteMap = new AdminSiteMap {
            AllPermissions = true, PermissionNames = new List<string>(), ActionName = "SampleAction",
            ControllerName = "SampleController"
        };
        _mockAdminSiteMapService.Setup(s => s.GetSiteMap()).ReturnsAsync(new List<AdminSiteMap> { menuSiteMap });
        _mockPermissionService.Setup(s => s.Authorize(It.IsAny<string>(), It.IsAny<Customer>())).ReturnsAsync(false);

        _securityConfig.AuthorizeAdminMenu = true;
        var attribute = new AuthorizeMenuAttribute();
        var filter = new AuthorizeMenuAttribute.AuthorizeMenuFilter(
            false,
            _mockPermissionService.Object,
            _mockAdminSiteMapService.Object,
            _mockWorkContext.Object,
            _securityConfig
        );

        // Act
        await filter.OnAuthorizationAsync(_mockFilterContext);

        // Assert
        // No permissions in the sitemap, so no redirect should occur
        Assert.IsNull(_mockFilterContext.Result);
    }

    [TestMethod]
    public async Task TestAuthorizeMenuAttribute_WithoutAllPermissions_Authorized()
    {
        // Arrange
        var menuSiteMap = new AdminSiteMap {
            AllPermissions = false, PermissionNames = new List<string> { "Permission1" }, ActionName = "SampleAction",
            ControllerName = "SampleController"
        };
        _mockAdminSiteMapService.Setup(s => s.GetSiteMap()).ReturnsAsync(new List<AdminSiteMap> { menuSiteMap });
        _mockPermissionService.Setup(s => s.Authorize(It.IsAny<string>(), It.IsAny<Customer>())).ReturnsAsync(true);
        _securityConfig.AuthorizeAdminMenu = true;

        var attribute = new AuthorizeMenuAttribute();
        var filter = new AuthorizeMenuAttribute.AuthorizeMenuFilter(
            false,
            _mockPermissionService.Object,
            _mockAdminSiteMapService.Object,
            _mockWorkContext.Object,
            _securityConfig
        );

        // Act
        await filter.OnAuthorizationAsync(_mockFilterContext);

        // Assert
        // Permission exists and user is authorized, so no redirect should occur
        Assert.IsNull(_mockFilterContext.Result);
    }

    [TestMethod]
    public async Task TestAuthorizeMenuAttribute_WithoutAllPermissions_NotAuthorized()
    {
        // Arrange
        var menuSiteMap = new AdminSiteMap {
            AllPermissions = false, PermissionNames = new List<string> { "Permission1" }, ActionName = "SampleAction",
            ControllerName = "SampleController"
        };
        _mockAdminSiteMapService.Setup(s => s.GetSiteMap()).ReturnsAsync(new List<AdminSiteMap> { menuSiteMap });
        _mockPermissionService.Setup(s => s.Authorize(It.IsAny<string>(), It.IsAny<Customer>())).ReturnsAsync(false);
        _securityConfig.AuthorizeAdminMenu = true;

        var attribute = new AuthorizeMenuAttribute();
        var filter = new AuthorizeMenuAttribute.AuthorizeMenuFilter(
            false,
            _mockPermissionService.Object,
            _mockAdminSiteMapService.Object,
            _mockWorkContext.Object,
            _securityConfig
        );

        // Act
        await filter.OnAuthorizationAsync(_mockFilterContext);

        // Assert
        // Permission exists but user is not authorized, so a redirect should occur
        Assert.IsInstanceOfType(_mockFilterContext.Result, typeof(ForbidResult));
    }
}