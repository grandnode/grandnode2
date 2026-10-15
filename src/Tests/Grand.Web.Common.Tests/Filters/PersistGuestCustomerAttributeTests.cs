using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Affiliates;
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

namespace Grand.Web.Common.Tests.Filters;

[TestClass]
public class PersistGuestCustomerAttributeTests
{
    private readonly List<string> _order = [];
    private Mock<IGuestCustomerPersistence> _persistence;

    [TestInitialize]
    public void Setup()
    {
        //the test App_Data/Settings.cfg has a connection string, so the filters see an installed database
        DataSettingsManager.Initialize(Path.Combine("", CommonPath.AppData, CommonPath.SettingsFile));
        _persistence = new Mock<IGuestCustomerPersistence>();
        _persistence.Setup(p => p.EnsurePersisted()).Callback(() => _order.Add("persist"))
            .Returns(Task.CompletedTask);
    }

    private static (ActionExecutingContext, ActionExecutionDelegate) Context(string query, Action onAction)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.QueryString = new QueryString(query);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(),
            new Dictionary<string, object>(), null);
        return (executing, () =>
        {
            onAction();
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null));
        });
    }

    [TestMethod]
    public async Task PersistsTheGuestBeforeTheAction()
    {
        var (executing, next) = Context("", () => _order.Add("action"));
        var filter = new PersistGuestCustomerAttribute.PersistGuestCustomerFilter(_persistence.Object);

        await filter.OnActionExecutionAsync(executing, next);

        CollectionAssert.AreEqual(new[] { "persist", "action" }, _order);
    }

    [TestMethod]
    public async Task Affiliate_LandingWithAffiliateId_PersistsThenWrites()
    {
        var customer = new Customer { Id = "g1" };
        var (filter, customerService) = AffiliateFilter(customer,
            new Affiliate { Id = "aff", Active = true });
        customerService.Setup(c => c.UpdateCustomerField("g1", It.IsAny<Expression<Func<Customer, string>>>(), "aff"))
            .Callback(() => _order.Add("write")).Returns(Task.CompletedTask);
        var (executing, next) = Context("?affiliateid=aff", () => { });

        await filter.OnActionExecutionAsync(executing, next);

        CollectionAssert.AreEqual(new[] { "persist", "write" }, _order);
    }

    [TestMethod]
    public async Task Affiliate_InactiveAffiliate_NothingPersisted()
    {
        var (filter, _) = AffiliateFilter(new Customer { Id = "g1" }, new Affiliate { Id = "aff", Active = false });
        var (executing, next) = Context("?affiliateid=aff", () => { });

        await filter.OnActionExecutionAsync(executing, next);

        _persistence.Verify(p => p.EnsurePersisted(), Times.Never);
    }

    private (AffiliateAttribute.AffiliateFilter, Mock<ICustomerService>) AffiliateFilter(Customer current,
        Affiliate affiliate)
    {
        var workContext = new Mock<IWorkContext>();
        workContext.SetupGet(w => w.CurrentCustomer).Returns(current);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.SetupGet(c => c.WorkContext).Returns(workContext.Object);
        var affiliateService = new Mock<IAffiliateService>();
        affiliateService.Setup(a => a.GetAffiliateById(affiliate.Id)).ReturnsAsync(affiliate);
        var customerService = new Mock<ICustomerService>();

        return (new AffiliateAttribute.AffiliateFilter(affiliateService.Object, customerService.Object,
            contextAccessor.Object, _persistence.Object), customerService);
    }
}
