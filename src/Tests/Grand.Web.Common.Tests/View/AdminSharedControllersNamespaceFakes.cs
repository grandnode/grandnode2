// Stands in for a real Base*Controller living in Grand.Web.AdminShared.Controllers
// (this test project has no reference to Grand.Web.AdminShared) — only the
// namespace string matters to ViewLocationExpander.IsAdminSharedController.
namespace Grand.Web.AdminShared.Controllers
{
    public abstract class FakeBaseController { }
}

namespace Grand.Web.Common.Tests.View
{
    public class FakeAdminSharedSubclass : Grand.Web.AdminShared.Controllers.FakeBaseController { }

    public class FakeUnrelatedController { }
}
