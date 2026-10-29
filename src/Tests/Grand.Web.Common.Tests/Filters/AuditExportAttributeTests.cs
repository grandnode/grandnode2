using Grand.Web.Common.Audit;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Filters;

[TestClass]
public class AuditExportAttributeTests
{
    private Mock<IAuditEventRecorder> _recorder;

    [TestInitialize]
    public void Setup()
    {
        _recorder = new Mock<IAuditEventRecorder>();
    }

    private async Task Run(IActionResult result, Dictionary<string, object> arguments, Exception exception = null)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), arguments, null);
        var filter = new AuditExportAttribute.AuditExportFilter("Customer", _recorder.Object);

        await filter.OnActionExecutionAsync(executing, () => Task.FromResult(
            new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null) {
                Result = result, Exception = exception
            }));
    }

    [TestMethod]
    public async Task FileResult_RecordsExportWithFilterSummary()
    {
        var model = new ExportFilter { SearchEmail = "vip", CustomerGroups = ["g1", "g2"], Password = "x" };

        await Run(new FileContentResult([1, 2], "text/csv"),
            new Dictionary<string, object> { { "model", model }, { "selectedIds", "a,b,c" } });

        _recorder.Verify(r => r.Export("Customer",
            It.Is<string>(s => s.Contains("SearchEmail=vip") && s.Contains("CustomerGroups: 2 items")
                               && s.Contains("selectedIds=a,b,c") && !s.Contains("Password")),
            null), Times.Once);
    }

    [TestMethod]
    public async Task NonFileResult_NotRecorded()
    {
        await Run(new RedirectToActionResult("List", null, null), new Dictionary<string, object>());

        _recorder.Verify(r => r.Export(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [TestMethod]
    public async Task ActionFailed_NotRecorded()
    {
        await Run(null, new Dictionary<string, object>(), new InvalidOperationException());

        _recorder.Verify(r => r.Export(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [TestMethod]
    public async Task UnreadableModel_ExportStillRecorded()
    {
        await Run(new FileContentResult([1], "text/csv"),
            new Dictionary<string, object> { { "model", new UnreadableFilter() } });

        _recorder.Verify(r => r.Export("Customer", "unreadable", null), Times.Once);
    }

    [TestMethod]
    public async Task IndexerProperty_Skipped()
    {
        await Run(new FileContentResult([1], "text/csv"),
            new Dictionary<string, object> { { "model", new IndexedFilter { Name = "vip" } } });

        _recorder.Verify(r => r.Export("Customer", "Name=vip", null), Times.Once);
    }

    public class UnreadableFilter
    {
        public string Broken => throw new InvalidOperationException();
    }

    public class IndexedFilter
    {
        public string Name { get; set; }
        public string this[int index] => Name;
    }

    public class ExportFilter
    {
        public string SearchEmail { get; set; }
        public string SearchName { get; set; }
        public IList<string> CustomerGroups { get; set; }
        public string Password { get; set; }
        public int PageSize { get; set; }
    }
}
