using Grand.Module.Scim.Infrastructure;
using Grand.Module.Scim.Models;
using Grand.Module.Scim.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Grand.Modules.Tests.Scim;

[TestClass]
public class ScimGroupPatchTests
{
    private static ScimGroupChanges Apply(string operationsJson)
    {
        var ops = JsonSerializer.Deserialize<ScimPatchRequest>("{\"Operations\":" + operationsJson + "}", ScimJson.Options)
            .Operations;
        Assert.IsTrue(ScimGroupPatch.TryApply(ops, out var changes, out var error), error);
        return changes;
    }

    [TestMethod]
    public void Entra_AddMembers()
    {
        var changes = Apply("[{\"op\":\"Add\",\"path\":\"members\",\"value\":[{\"value\":\"u1\"},{\"value\":\"u2\"}]}]");
        CollectionAssert.AreEqual(new[] { "u1", "u2" }, changes.Add.ToList());
        Assert.IsFalse(changes.ReplaceAll);
        Assert.IsTrue(changes.MembersChanged);
    }

    [TestMethod]
    public void Entra_RemoveMembersByValue()
    {
        var changes = Apply("[{\"op\":\"Remove\",\"path\":\"members\",\"value\":[{\"value\":\"u1\"}]}]");
        CollectionAssert.AreEqual(new[] { "u1" }, changes.Remove.ToList());
    }

    [TestMethod]
    public void Okta_RemoveMemberByFilterPath()
    {
        var changes = Apply("[{\"op\":\"remove\",\"path\":\"members[value eq \\\"u7\\\"]\"}]");
        CollectionAssert.AreEqual(new[] { "u7" }, changes.Remove.ToList());
    }

    [TestMethod]
    public void RemoveAllMembers()
    {
        var changes = Apply("[{\"op\":\"remove\",\"path\":\"members\"}]");
        Assert.IsTrue(changes.ReplaceAll);
        Assert.AreEqual(0, changes.Add.Count);
    }

    [TestMethod]
    public void ReplaceMembers()
    {
        var changes = Apply("[{\"op\":\"replace\",\"path\":\"members\",\"value\":[{\"value\":\"u3\"}]}]");
        Assert.IsTrue(changes.ReplaceAll);
        CollectionAssert.AreEqual(new[] { "u3" }, changes.Add.ToList());
    }

    [TestMethod]
    public void Okta_ReplaceWithoutPath_Rename()
    {
        var changes = Apply("[{\"op\":\"replace\",\"value\":{\"id\":\"g1\",\"displayName\":\"New name\"}}]");
        Assert.AreEqual("New name", changes.DisplayName);
        Assert.IsFalse(changes.MembersChanged);
    }

    [TestMethod]
    public void ReplaceDisplayNamePath() =>
        Assert.AreEqual("Renamed", Apply("[{\"op\":\"Replace\",\"path\":\"displayName\",\"value\":\"Renamed\"}]").DisplayName);

    [TestMethod]
    public void UnknownPath_Refused()
    {
        var ops = JsonSerializer.Deserialize<ScimPatchRequest>(
            "{\"Operations\":[{\"op\":\"add\",\"path\":\"owners\",\"value\":[]}]}", ScimJson.Options).Operations;
        Assert.IsFalse(ScimGroupPatch.TryApply(ops, out _, out _));
    }
}
