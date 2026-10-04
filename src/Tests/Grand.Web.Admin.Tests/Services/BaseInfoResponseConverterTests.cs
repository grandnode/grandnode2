using elFinder.Net.Core.Models.FileInfo;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Services;

/// <summary>
///     OpenResponse.cwd is declared as BaseInfoResponse, so these tests serialize through that same
///     declared type - the condition under which the properties go missing.
/// </summary>
[TestClass]
public class BaseInfoResponseConverterTests
{
    private JsonSerializerOptions _options;

    [TestInitialize]
    public void Setup()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new BaseInfoResponseConverter());
    }

    /// <summary>
    ///     Losing phash costs the browser the link from the current directory to its parent - and with
    ///     it every file URL below the volume root, because elFinder builds those by walking that chain.
    /// </summary>
    [TestMethod]
    public void Directory_KeepsTheParentHashOfItsRuntimeType()
    {
        BaseInfoResponse cwd = new DirectoryInfoResponse {
            name = "test", hash = "v1_XHRlc3Q", phash = "v1_", volumeid = "v1_", mime = "directory"
        };

        var json = JsonSerializer.Serialize(cwd, _options);

        StringAssert.Contains(json, "\"phash\":\"v1_\"");
        StringAssert.Contains(json, "\"volumeid\":\"v1_\"");
    }

    [TestMethod]
    public void Root_KeepsTheRootMarkerOfItsRuntimeType()
    {
        BaseInfoResponse cwd = new RootInfoResponse {
            name = "Volume", hash = "v1_", volumeid = "v1_", isroot = 1, mime = "directory"
        };

        var json = JsonSerializer.Serialize(cwd, _options);

        StringAssert.Contains(json, "\"isroot\":1");
    }

    [TestMethod]
    public void DeclaredTypeAloneStillDropsTheParentHash()
    {
        BaseInfoResponse cwd = new DirectoryInfoResponse { name = "test", hash = "v1_XHRlc3Q", phash = "v1_" };

        var json = JsonSerializer.Serialize(cwd, new JsonSerializerOptions());

        Assert.IsFalse(json.Contains("phash"), "Guards the premise of the converter - remove it and the fix is moot");
    }
}
