using Grand.Business.Core.Utilities.Authentication;
using Grand.Module.Scim.Infrastructure;
using Grand.Module.Scim.Models;
using Grand.Module.Scim.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Grand.Modules.Tests.Scim;

[TestClass]
public class ScimUserPatchTests
{
    private static readonly ScimUserData User = new("jan@example.com", "oid-1", null, "Jan", "Nowak", true);

    private static IList<ScimPatchOperation> Ops(string json) =>
        JsonSerializer.Deserialize<ScimPatchRequest>(json, ScimJson.Options).Operations;

    private static ScimUserData Apply(string operationsJson)
    {
        Assert.IsTrue(ScimUserPatch.TryApply(User, Ops("{\"Operations\":" + operationsJson + "}"), out var result,
            out var error), error);
        return result;
    }

    //Entra ID sends the boolean as a string with a capital letter, and capitalises the op
    [TestMethod]
    public void Entra_ReplaceActiveStringFalse_Deactivates() =>
        Assert.IsFalse(Apply("[{\"op\":\"Replace\",\"path\":\"active\",\"value\":\"False\"}]").Active);

    [TestMethod]
    public void Okta_ReplaceWithoutPath_ObjectValue()
    {
        var result = Apply("[{\"op\":\"replace\",\"value\":{\"active\":false,\"name\":{\"givenName\":\"Janek\"}}}]");
        Assert.IsFalse(result.Active);
        Assert.AreEqual("Janek", result.GivenName);
        Assert.AreEqual("Nowak", result.FamilyName);
    }

    [TestMethod]
    public void ReplaceBooleanTrue() =>
        Assert.IsTrue(Apply("[{\"op\":\"replace\",\"path\":\"active\",\"value\":true}]").Active);

    [TestMethod]
    public void NamePaths()
    {
        var result = Apply("[{\"op\":\"Add\",\"path\":\"name.givenName\",\"value\":\"Anna\"}," +
                           "{\"op\":\"Replace\",\"path\":\"name.familyName\",\"value\":\"Kowalska\"}]");
        Assert.AreEqual("Anna", result.GivenName);
        Assert.AreEqual("Kowalska", result.FamilyName);
    }

    [TestMethod]
    public void WorkEmailPath() =>
        Assert.AreEqual("new@example.com",
            Apply("[{\"op\":\"Replace\",\"path\":\"emails[type eq \\\"work\\\"].value\",\"value\":\"new@example.com\"}]")
                .Email);

    [TestMethod]
    public void UserNameAndExternalId()
    {
        var result = Apply("[{\"op\":\"replace\",\"path\":\"userName\",\"value\":\"anna@example.com\"}," +
                           "{\"op\":\"replace\",\"path\":\"externalId\",\"value\":\"oid-9\"}]");
        Assert.AreEqual("anna@example.com", result.UserName);
        Assert.AreEqual("oid-9", result.ExternalId);
    }

    [TestMethod]
    public void RemoveGivenName() =>
        Assert.IsNull(Apply("[{\"op\":\"remove\",\"path\":\"name.givenName\"}]").GivenName);

    //Entra's default mapping sends attributes GrandNode does not keep (title, department...): they are ignored
    [TestMethod]
    public void UnknownPath_Ignored() =>
        Assert.AreEqual(User, Apply("[{\"op\":\"Add\",\"path\":\"title\",\"value\":\"Engineer\"}]"));

    [TestMethod]
    public void UnknownOp_Refused() =>
        Assert.IsFalse(ScimUserPatch.TryApply(User, Ops("{\"Operations\":[{\"op\":\"move\",\"path\":\"active\"}]}"),
            out _, out _));

    [TestMethod]
    public void ActiveNotABoolean_Refused() =>
        Assert.IsFalse(ScimUserPatch.TryApply(User,
            Ops("{\"Operations\":[{\"op\":\"replace\",\"path\":\"active\",\"value\":\"maybe\"}]}"), out _, out _));
}
