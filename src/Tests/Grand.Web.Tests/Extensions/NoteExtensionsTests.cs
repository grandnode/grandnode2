using Grand.Infrastructure.Security;
using Grand.Web.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Extensions;

[TestClass]
public class NoteExtensionsTests
{
    private readonly Mock<IHtmlSanitizationService> _sanitizer = new();

    [TestMethod]
    public void CustomerNote_IsShownAsText()
    {
        _sanitizer.Setup(x => x.ContainsMarkup(It.IsAny<string>())).Returns(true);

        var html = _sanitizer.Object.NoteHtml("<img src=x onerror=alert(1)>", createdByCustomer: true);

        Assert.IsNotNull(html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;img", html);
    }

    [TestMethod]
    public void StaffNote_WithAllowedMarkup_IsKept()
    {
        _sanitizer.Setup(x => x.ContainsMarkup(It.IsAny<string>())).Returns(true);
        _sanitizer.Setup(x => x.ContainsDisallowedRichText(It.IsAny<string>())).Returns(false);

        Assert.AreEqual("<a href=\"/form\">form</a>",
            _sanitizer.Object.NoteHtml("<a href=\"/form\">form</a>", createdByCustomer: false));
    }

    [TestMethod]
    public void StaffNote_WithDisallowedMarkup_IsShownAsText()
    {
        _sanitizer.Setup(x => x.ContainsMarkup(It.IsAny<string>())).Returns(true);
        _sanitizer.Setup(x => x.ContainsDisallowedRichText(It.IsAny<string>())).Returns(true);

        var html = _sanitizer.Object.NoteHtml("<script>x</script>", createdByCustomer: false);

        Assert.IsNotNull(html);
        Assert.DoesNotContain("<script", html);
    }
}
