using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.RequestCustomerResolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;
using System.Net;

namespace Grand.Web.Common.Tests.Filters;

[TestClass]
public class CustomerActivityAttributeTests
{
    private Mock<ICustomerService> _customerService;
    private Mock<IContextAccessor> _contextAccessor;
    private Mock<IWorkContext> _workContext;
    private Mock<IGuestCustomerPersistence> _persistence;

    [TestInitialize]
    public void Setup()
    {
        //the test App_Data/Settings.cfg has a connection string, so the filter sees an installed database
        DataSettingsManager.Initialize(Path.Combine("", CommonPath.AppData, CommonPath.SettingsFile));
        _customerService = new Mock<ICustomerService>();
        _workContext = new Mock<IWorkContext>();
        _persistence = new Mock<IGuestCustomerPersistence>();
        _contextAccessor = new Mock<IContextAccessor>();
        _contextAccessor.SetupGet(c => c.WorkContext).Returns(_workContext.Object);
    }

    private async Task RunGet(Customer current)
    {
        _workContext.SetupGet(w => w.CurrentCustomer).Returns(current);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("shop.test");
        httpContext.Request.Path = "/product";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(),
            new Dictionary<string, object>(), null);
        var filter = new CustomerActivityAttribute.CustomerActivityFilter(_customerService.Object,
            _contextAccessor.Object, new CustomerSettings { StoreLastVisitedPage = true }, _persistence.Object);

        await filter.OnActionExecutionAsync(executing,
            () => Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null)));
    }

    [TestMethod]
    public async Task SystemAccount_NothingWritten()
    {
        await RunGet(new Customer { Id = "searchengine", IsSystemAccount = true });

        _customerService.Verify(c => c.UpdateCustomerField(It.IsAny<Customer>(),
            It.IsAny<Expression<Func<Customer, DateTime>>>(), It.IsAny<DateTime>()), Times.Never);
        _customerService.Verify(c => c.UpdateCustomerField(It.IsAny<Customer>(),
            It.IsAny<Expression<Func<Customer, string>>>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Guest_ActivityWritten()
    {
        await RunGet(new Customer { Id = "guest", Active = true });

        _customerService.Verify(c => c.UpdateCustomerField(It.IsAny<Customer>(),
            It.IsAny<Expression<Func<Customer, string>>>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task TransientGuest_NothingWritten()
    {
        _persistence.Setup(p => p.IsTransient("unsaved")).Returns(true);

        await RunGet(new Customer { Id = "unsaved", Active = true });

        _customerService.Verify(c => c.UpdateCustomerField(It.IsAny<Customer>(),
            It.IsAny<Expression<Func<Customer, string>>>(), It.IsAny<string>()), Times.Never);
    }
}
