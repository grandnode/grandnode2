using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.SharedKernel.Tests.Extensions;

[TestClass]
public class FileExtensionsTests
{
    [TestMethod]
    public void GetAllowedMediaFileTypes_Empty_DefaultImageTypes()
    {
        var result = FileExtensions.GetAllowedMediaFileTypes(null);

        CollectionAssert.AreEquivalent(new[] { ".gif", ".jpg", ".jpeg", ".png", ".bmp", ".webp" }, result.ToArray());
    }

    [TestMethod]
    public void GetAllowedMediaFileTypes_TrailingAndDoubledComma_NoEmptyEntry()
    {
        var result = FileExtensions.GetAllowedMediaFileTypes(" .JPG, .png,, ,");

        CollectionAssert.AreEqual(new[] { ".jpg", ".png" }, result.ToArray());
    }

    [TestMethod]
    [DataRow(".jpg", true)]
    [DataRow(".PNG", true)]
    [DataRow(".svg", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void IsAllowedMediaFileType_ChecksTheExtension(string extension, bool expected)
    {
        var allowed = FileExtensions.GetAllowedMediaFileTypes(".jpg,.png,");

        Assert.AreEqual(expected, allowed.IsAllowedMediaFileType(extension));
    }
}
