using Grand.Data.Audit;
using Grand.Domain.Audit;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Audit;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Security.Claims;

namespace Grand.Web.Common.Tests.Filters;

[TestClass]
public class AuditScopeFilterTests
{
    private Mock<IAuditTracker> _tracker;
    private Mock<IWorkContext> _workContext;
    private Mock<IContextAccessor> _contextAccessor;
    private AuditActor _activated;

    private static readonly Customer Staff = new() {
        Id = "staff1", Email = "anna@shop.com", StaffStoreId = "s1", VendorId = "v1"
    };

    [TestInitialize]
    public void Setup()
    {
        _tracker = new Mock<IAuditTracker>();
        _tracker.Setup(t => t.Activate(It.IsAny<AuditActor>())).Callback<AuditActor>(a => _activated = a);
        _workContext = new Mock<IWorkContext>();
        _workContext.SetupGet(w => w.CurrentCustomer).Returns(Staff);
        _contextAccessor = new Mock<IContextAccessor>();
        _contextAccessor.SetupGet(c => c.WorkContext).Returns(_workContext.Object);
    }

    private async Task Run(string area, bool authenticated = true, object[] metadata = null, bool throws = false)
    {
        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-1",
            User = authenticated
                ? new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "anna@shop.com")], "test"))
                : new ClaimsPrincipal(new ClaimsIdentity())
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        httpContext.Request.Headers.UserAgent = "UA";
        var routeData = new RouteData();
        if (area != null)
            routeData.Values["area"] = area;
        routeData.Values["controller"] = "Order";
        routeData.Values["action"] = "CancelOrder";
        var descriptor = new ActionDescriptor { EndpointMetadata = metadata ?? [] };
        var actionContext = new ActionContext(httpContext, routeData, descriptor);
        var executing = new ResourceExecutingContext(actionContext, new List<IFilterMetadata>(),
            new List<IValueProviderFactory>());
        var filter = new AuditScopeFilter(_tracker.Object, _contextAccessor.Object);

        await filter.OnResourceExecutionAsync(executing, () =>
        {
            if (throws)
                throw new InvalidOperationException("action failed");
            return Task.FromResult(new ResourceExecutedContext(actionContext, new List<IFilterMetadata>()));
        });
    }

    [TestMethod]
    public async Task StorefrontRequest_NotActivated()
    {
        await Run(null);

        _tracker.Verify(t => t.Activate(It.IsAny<AuditActor>()), Times.Never);
        _tracker.Verify(t => t.FlushAsync(), Times.Never);
    }

    [TestMethod]
    public async Task AdminArea_Authenticated_ActivatedWithActor()
    {
        await Run("Admin");

        Assert.IsNotNull(_activated);
        Assert.AreEqual(AuditSource.Admin, _activated.Source);
        Assert.AreEqual("staff1", _activated.CustomerId);
        Assert.AreEqual("anna@shop.com", _activated.Email);
        Assert.AreEqual("s1", _activated.StoreId);
        Assert.AreEqual("v1", _activated.VendorId);
        Assert.AreEqual("10.0.0.1", _activated.IpAddress);
        Assert.AreEqual("UA", _activated.UserAgent);
        Assert.AreEqual("trace-1", _activated.OperationId);
        Assert.IsNull(_activated.ImpersonatedCustomerId);
        _tracker.Verify(t => t.FlushAsync(), Times.Once);
    }

    [TestMethod]
    public async Task Actor_ActionIsAreaControllerAction()
    {
        await Run("Admin");

        Assert.AreEqual("Admin / Order / CancelOrder", _activated.Action);
    }

    [TestMethod]
    public async Task StoreArea_SourceStore()
    {
        await Run("Store");

        Assert.AreEqual(AuditSource.Store, _activated.Source);
    }

    [TestMethod]
    public async Task VendorArea_SourceVendor()
    {
        await Run("Vendor");

        Assert.AreEqual(AuditSource.Vendor, _activated.Source);
    }

    [TestMethod]
    public async Task AdminArea_Anonymous_NotActivated()
    {
        await Run("Admin", false);

        _tracker.Verify(t => t.Activate(It.IsAny<AuditActor>()), Times.Never);
    }

    [TestMethod]
    public async Task ApiAttribute_SourceApi()
    {
        await Run(null, metadata: [new AuditSourceAttribute(AuditSource.Api)]);

        Assert.AreEqual(AuditSource.Api, _activated.Source);
        Assert.AreEqual("Api / Order / CancelOrder", _activated.Action);
    }

    [TestMethod]
    public async Task ScimWithoutCustomer_ActorScim()
    {
        _workContext.SetupGet(w => w.CurrentCustomer).Returns(new Customer { Id = "guest" });

        //SCIM checks its bearer token in ScimAuthorizeFilter, not through an authentication scheme
        await Run(null, false, [new AuditSourceAttribute(AuditSource.Scim)]);

        Assert.AreEqual(AuditSource.Scim, _activated.Source);
        Assert.AreEqual("SCIM", _activated.Email);
        Assert.IsNull(_activated.CustomerId);
    }

    [TestMethod]
    public async Task ApiAttribute_Anonymous_NotActivated()
    {
        await Run(null, false, [new AuditSourceAttribute(AuditSource.Api)]);

        _tracker.Verify(t => t.Activate(It.IsAny<AuditActor>()), Times.Never);
    }

    [TestMethod]
    public async Task ActionThrows_FlushStillCalled()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Run("Admin", throws: true));

        _tracker.Verify(t => t.FlushAsync(), Times.Once);
    }

    [TestMethod]
    public async Task Impersonating_ActorIsOriginal_ImpersonatedIdSet()
    {
        _workContext.SetupGet(w => w.CurrentCustomer).Returns(new Customer { Id = "cust9", Email = "buyer@x.com" });
        _workContext.SetupGet(w => w.OriginalCustomerIfImpersonated).Returns(Staff);

        await Run("Admin");

        Assert.AreEqual("staff1", _activated.CustomerId);
        Assert.AreEqual("anna@shop.com", _activated.Email);
        Assert.AreEqual("cust9", _activated.ImpersonatedCustomerId);
    }

    [TestMethod]
    public async Task Storefront_Impersonating_ActivatedSourceStorefront()
    {
        _workContext.SetupGet(w => w.CurrentCustomer).Returns(new Customer { Id = "cust9", Email = "buyer@x.com" });
        _workContext.SetupGet(w => w.OriginalCustomerIfImpersonated).Returns(Staff);

        await Run(null);

        Assert.AreEqual(AuditSource.Storefront, _activated.Source);
        Assert.AreEqual("staff1", _activated.CustomerId);
        Assert.AreEqual("cust9", _activated.ImpersonatedCustomerId);
    }

    [TestMethod]
    public async Task Storefront_NotImpersonating_NotActivated()
    {
        _workContext.SetupGet(w => w.CurrentCustomer).Returns(new Customer { Id = "cust9", Email = "buyer@x.com" });

        await Run(null);

        _tracker.Verify(t => t.Activate(It.IsAny<AuditActor>()), Times.Never);
    }
}
