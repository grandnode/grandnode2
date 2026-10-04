using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.SharedKernel.Tests.Extensions;

[TestClass]
public class FormatTextTests
{
    [TestMethod]
    [DataRow("sample <br /> sample ")]
    [DataRow("&nbsp;&nbsp; sample <br /> sample &nbsp;&nbsp;")]
    public void ConvertTextTest(string text)
    {
        Assert.DoesNotContain("<br", FormatText.ConvertText(text));
        Assert.DoesNotContain("&nbsp;&nbsp;", FormatText.ConvertText(text));
    }
}