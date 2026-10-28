using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Audit;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Audit;
using Grand.Web.Common.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;

namespace Grand.Web.Common.Tests.Audit;

[TestClass]
public class AuditEventRecorderTests
{
    private static readonly Customer Staff = new() { Id = "staff1", Email = "anna@shop.com", StaffStoreId = "s1" };
    private static readonly Customer Buyer = new() { Id = "cust9", Email = "buyer@x.com" };

    private Mock<IAuditService> _auditService;
    private Mock<IGroupService> _groupService;
    private Mock<IWorkContext> _workContext;
    private AuditSettings _settings;
    private DefaultHttpContext _httpContext;
    private List<AuditEntry> _recorded;
    private AuditEventRecorder _recorder;

    [TestInitialize]
    public void Init()
    {
        _recorded = [];
        _auditService = new Mock<IAuditService>();
        _auditService.Setup(s => s.InsertEvent(It.IsAny<AuditEntry>()))
            .Callback<AuditEntry>(e => _recorded.Add(e))
            .Returns(Task.CompletedTask);
        _groupService = new Mock<IGroupService>();
        _groupService.Setup(g => g.IsAdmin(It.Is<Customer>(c => c.Id == Staff.Id))).ReturnsAsync(true);
        _workContext = new Mock<IWorkContext>();
        _workContext.SetupGet(w => w.CurrentCustomer).Returns(Staff);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.SetupGet(c => c.WorkContext).Returns(_workContext.Object);
        _settings = new AuditSettings();
        _httpContext = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        _httpContext.Request.Headers.UserAgent = "UA";
        _httpContext.Request.RouteValues["area"] = "Admin";
        _httpContext.Request.RouteValues["controller"] = "Customer";
        _httpContext.Request.RouteValues["action"] = "ExportExcelAll";
        _recorder = new AuditEventRecorder(_auditService.Object, _settings, _groupService.Object,
            new HttpContextAccessor { HttpContext = _httpContext }, contextAccessor.Object,
            NullLogger<AuditEventRecorder>.Instance);
    }

    [TestMethod]
    public async Task SignIn_Staff_Recorded()
    {
        await _recorder.SignIn(Staff);

        var entry = _recorded.Single();
        Assert.AreEqual(AuditKind.SignIn, entry.Kind);
        Assert.AreEqual(AuditSource.Admin, entry.Source);
        Assert.AreEqual("staff1", entry.ActorCustomerId);
        Assert.AreEqual("anna@shop.com", entry.ActorEmail);
        Assert.AreEqual("s1", entry.StoreId);
        Assert.AreEqual("10.0.0.1", entry.IpAddress);
        Assert.AreEqual("UA", entry.UserAgent);
        Assert.AreEqual("trace-1", entry.OperationId);
        Assert.AreEqual("Customer", entry.EntityType);
        Assert.AreEqual("staff1", entry.EntityId);
    }

    [TestMethod]
    public async Task SignIn_Storefront_SourceStorefront()
    {
        _httpContext.Request.RouteValues.Remove("area");

        await _recorder.SignIn(Staff);

        Assert.AreEqual(AuditSource.Storefront, _recorded.Single().Source);
    }

    [TestMethod]
    public async Task Event_MarkedEndpoint_SourceFromMark()
    {
        //an API or SCIM request: its data changes and its events must agree on the source
        _httpContext.Request.RouteValues.Remove("area");
        _httpContext.SetEndpoint(new Endpoint(null,
            new EndpointMetadataCollection(new AuditSourceAttribute(AuditSource.Api)), "api"));

        await _recorder.Export("Product", null, null);

        var entry = _recorded.Single();
        Assert.AreEqual(AuditSource.Api, entry.Source);
        Assert.AreEqual("Api / Customer / ExportExcelAll", entry.Action);
    }

    [TestMethod]
    public async Task SignIn_Customer_NotRecorded()
    {
        await _recorder.SignIn(Buyer);

        Assert.AreEqual(0, _recorded.Count);
    }

    [TestMethod]
    public async Task SignIn_SettingOff_NotRecorded()
    {
        _settings.LogStaffSignIns = false;

        await _recorder.SignIn(Staff);
        await _recorder.SignInFailed(Staff, "wrong password");
        await _recorder.SignOut(Staff);

        Assert.AreEqual(0, _recorded.Count);
    }

    [TestMethod]
    public async Task SignInFailed_Customer_NotRecorded()
    {
        await _recorder.SignInFailed(Buyer, "wrong password");

        Assert.AreEqual(0, _recorded.Count);
    }

    [TestMethod]
    public async Task SignInFailed_Staff_RecordedWithReason()
    {
        await _recorder.SignInFailed(Staff, "wrong password");

        var entry = _recorded.Single();
        Assert.AreEqual(AuditKind.SignInFailed, entry.Kind);
        Assert.AreEqual("wrong password", entry.Details);
        Assert.AreEqual("staff1", entry.ActorCustomerId);
    }

    [TestMethod]
    public async Task SignOut_Staff_Recorded()
    {
        await _recorder.SignOut(Staff);

        Assert.AreEqual(AuditKind.SignOut, _recorded.Single().Kind);
    }

    [TestMethod]
    public async Task SignOutCurrent_RecordsCurrentStaff()
    {
        await _recorder.SignOutCurrent();

        var entry = _recorded.Single();
        Assert.AreEqual(AuditKind.SignOut, entry.Kind);
        Assert.AreEqual("staff1", entry.ActorCustomerId);
    }

    [TestMethod]
    public async Task Export_RecordsNameFilterCount()
    {
        await _recorder.Export("Customer", "group=VIP", 1240);

        var entry = _recorded.Single();
        Assert.AreEqual(AuditKind.Export, entry.Kind);
        Assert.AreEqual("Customer", entry.EntityType);
        Assert.AreEqual("staff1", entry.ActorCustomerId);
        StringAssert.Contains(entry.Details, "group=VIP");
        StringAssert.Contains(entry.Details, "1240");
        Assert.AreEqual("Admin / Customer / ExportExcelAll", entry.Action);
    }

    [TestMethod]
    public async Task ImpersonationStart_SetsImpersonatedId()
    {
        await _recorder.ImpersonationStart(Buyer);

        var entry = _recorded.Single();
        Assert.AreEqual(AuditKind.ImpersonationStart, entry.Kind);
        Assert.AreEqual("staff1", entry.ActorCustomerId);
        Assert.AreEqual("cust9", entry.ImpersonatedCustomerId);
        Assert.AreEqual("cust9", entry.EntityId);
    }

    [TestMethod]
    public async Task ImpersonationEnd_ActorIsOriginal()
    {
        _httpContext.Request.RouteValues.Remove("area");
        _workContext.SetupGet(w => w.CurrentCustomer).Returns(Buyer);
        _workContext.SetupGet(w => w.OriginalCustomerIfImpersonated).Returns(Staff);

        await _recorder.ImpersonationEnd(Buyer);

        var entry = _recorded.Single();
        Assert.AreEqual(AuditKind.ImpersonationEnd, entry.Kind);
        Assert.AreEqual("staff1", entry.ActorCustomerId);
        Assert.AreEqual("cust9", entry.ImpersonatedCustomerId);
    }

    [TestMethod]
    public async Task AuditLogViewed_And_Exported_Recorded()
    {
        await _recorder.AuditLogViewed();
        await _recorder.AuditLogExported("entity type=Product", 12);

        CollectionAssert.AreEqual(new[] { AuditKind.AuditLogViewed, AuditKind.AuditLogExported },
            _recorded.Select(e => e.Kind).ToArray());
        StringAssert.Contains(_recorded[1].Details, "12");
    }

    [TestMethod]
    public async Task InsertThrows_Swallowed()
    {
        _auditService.Setup(s => s.InsertEvent(It.IsAny<AuditEntry>())).ThrowsAsync(new Exception("db down"));

        await _recorder.SignIn(Staff);
        await _recorder.Export("Customer", null, null);
    }

    [TestMethod]
    public async Task SignInEventHandler_ForwardsEvents()
    {
        var recorder = new Mock<IAuditEventRecorder>();
        var handler = new AuditSignInEventHandler(recorder.Object);

        await handler.Handle(new CustomerLoggedInEvent(Staff, null, "s1", null), CancellationToken.None);
        await handler.Handle(new CustomerLoginFailedEvent(Staff, "wrong two-factor code"), CancellationToken.None);
        await handler.Handle(new CustomerLoggedOutEvent(Staff), CancellationToken.None);

        recorder.Verify(r => r.SignIn(Staff));
        recorder.Verify(r => r.SignInFailed(Staff, "wrong two-factor code"));
        recorder.Verify(r => r.SignOut(Staff));
    }
}
