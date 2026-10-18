using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Security;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Module.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Filters;

[TestClass]
public class AuthorizeApiAdminAttributeTests
{
    private readonly Customer _customer = new();
    private Mock<IContextAccessor> _contextAccessor;
    private Mock<IPermissionService> _permissionService;

    [TestInitialize]
    public void Init()
    {
        DataSettingsManager.Initialize(Path.Join(Path.GetTempPath(), "grand-api-tests-settings.json"));
        DataSettingsManager.Instance.LoadDataSettings(new DataSettings { ConnectionString = "mongodb://localhost" });
        _permissionService = new Mock<IPermissionService>();
        _contextAccessor = new Mock<IContextAccessor>();
        _contextAccessor.Setup(x => x.WorkContext.CurrentCustomer).Returns(_customer);
    }

    private async Task<AuthorizationFilterContext> Run(bool apiEnabled, bool ignore, bool permitted)
    {
        _permissionService.Setup(x => x.Authorize(It.IsAny<Permission>(), _customer)).ReturnsAsync(permitted);
        var attribute = new AuthorizeApiAdminAttribute(ignore);
        var filter = (IAsyncAuthorizationFilter)ActivatorUtilities.CreateInstance(
            new ServiceCollection()
                .AddSingleton(_permissionService.Object)
                .AddSingleton(_contextAccessor.Object)
                .AddSingleton(new SecuritySettings())
                .AddSingleton(new BackendAPIConfig { Enabled = apiEnabled })
                .BuildServiceProvider(),
            attribute.ImplementationType, ignore);
        var context = new AuthorizationFilterContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            [new FilterDescriptor(attribute, FilterScope.Controller).Filter]);
        context.Filters.Add((IFilterMetadata)filter);
        await filter.OnAuthorizationAsync(context);
        return context;
    }

    [TestMethod]
    public async Task ApiDisabled_Forbid()
    {
        var context = await Run(apiEnabled: false, ignore: false, permitted: true);
        Assert.IsInstanceOfType<ForbidResult>(context.Result);
        _permissionService.Verify(x => x.Authorize(It.IsAny<Permission>(), _customer), Times.Never);
    }

    [TestMethod]
    public async Task ApiDisabled_IgnoredAction_Forbid()
    {
        //ignore skips the admin panel checks, not the kill switch
        var context = await Run(apiEnabled: false, ignore: true, permitted: true);
        Assert.IsInstanceOfType<ForbidResult>(context.Result);
    }

    [TestMethod]
    public async Task ApiEnabled_IgnoredAction_NoPermissionCheck()
    {
        var context = await Run(apiEnabled: true, ignore: true, permitted: false);
        Assert.IsNull(context.Result);
        _permissionService.Verify(x => x.Authorize(It.IsAny<Permission>(), _customer), Times.Never);
    }

    [TestMethod]
    public async Task ApiEnabled_PermissionDenied_Forbid()
    {
        var context = await Run(apiEnabled: true, ignore: false, permitted: false);
        Assert.IsInstanceOfType<ForbidResult>(context.Result);
        _permissionService.Verify(x => x.Authorize(It.IsAny<Permission>(), _customer), Times.Once);
    }

    [TestMethod]
    public async Task ApiEnabled_PermissionGranted_Allowed()
    {
        var context = await Run(apiEnabled: true, ignore: false, permitted: true);
        Assert.IsNull(context.Result);
    }
}
