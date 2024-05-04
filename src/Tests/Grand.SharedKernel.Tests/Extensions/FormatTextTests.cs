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
        Assert.IsFalse(FormatText.ConvertText(text).Contains("<br"));
        Assert.IsFalse(FormatText.ConvertText(text).Contains("&nbsp;&nbsp;"));
    }
}