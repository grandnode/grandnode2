using Grand.Data.Audit;
using Grand.Domain.Audit;
using Grand.Infrastructure.Audit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;

namespace Grand.Infrastructure.Tests.Audit;

[TestClass]
public class AuditDiffTests
{
    private static readonly ISet<string> NoIgnored = new HashSet<string>();

    private static IList<AuditChange> Compare(BsonDocument before, BsonDocument after, string entityType = "Product",
        ISet<string> ignored = null)
    {
        var isSetting = entityType == "Setting";
        return AuditDiff.Compare(entityType, AuditSnapshot.Flatten(before, isSetting),
            AuditSnapshot.Flatten(after, isSetting), ignored ?? NoIgnored);
    }

    [TestMethod]
    public void Compare_ScalarChanged_ReportsPathOldNew()
    {
        var changes = Compare(new BsonDocument("Price", 129), new BsonDocument("Price", 99));

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("Price", changes[0].Path);
        Assert.AreEqual("129", changes[0].OldValue);
        Assert.AreEqual("99", changes[0].NewValue);
        Assert.IsFalse(changes[0].Masked);
    }

    [TestMethod]
    public void Compare_Unchanged_ReturnsEmpty()
    {
        var doc = new BsonDocument { { "Name", "Shoes" }, { "Price", 10 } };

        Assert.AreEqual(0, Compare(doc, doc.DeepClone().AsBsonDocument).Count);
    }

    [TestMethod]
    public void Compare_IgnoredPath_NotReported()
    {
        var before = new BsonDocument { { "Name", "a" }, { "UpdatedOnUtc", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) } };
        var after = new BsonDocument { { "Name", "a" }, { "UpdatedOnUtc", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) } };

        Assert.AreEqual(0, Compare(before, after, ignored: new HashSet<string> { "UpdatedOnUtc" }).Count);
    }

    private static BsonDocument TierPrice(string id, int qty)
    {
        return new BsonDocument { { "_id", id }, { "Quantity", qty } };
    }

    [TestMethod]
    public void Compare_ArrayOfDocsWithId_MatchesById()
    {
        var before = new BsonDocument("TierPrices", new BsonArray { TierPrice("a3", 5), TierPrice("b7", 1) });
        var after = new BsonDocument("TierPrices", new BsonArray { TierPrice("b7", 1), TierPrice("a3", 10) });

        var changes = Compare(before, after);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("TierPrices[id=a3].Quantity", changes[0].Path);
        Assert.AreEqual("5", changes[0].OldValue);
        Assert.AreEqual("10", changes[0].NewValue);
    }

    [TestMethod]
    public void Compare_ArrayOfDocsWithId_AddedAndRemoved()
    {
        var before = new BsonDocument("TierPrices", new BsonArray { TierPrice("old", 1) });
        var after = new BsonDocument("TierPrices", new BsonArray { TierPrice("x", 2) });

        var changes = Compare(before, after);

        Assert.AreEqual(2, changes.Count);
        var added = changes.Single(c => c.Path == "TierPrices[id=x].Quantity");
        Assert.IsNull(added.OldValue);
        Assert.AreEqual("2", added.NewValue);
        var removed = changes.Single(c => c.Path == "TierPrices[id=old].Quantity");
        Assert.AreEqual("1", removed.OldValue);
        Assert.IsNull(removed.NewValue);
    }

    [TestMethod]
    public void Compare_ScalarArray_OneValue()
    {
        var changes = Compare(new BsonDocument("Stores", new BsonArray { "a", "b" }),
            new BsonDocument("Stores", new BsonArray { "b", "c" }));

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("Stores", changes[0].Path);
        Assert.AreEqual("[\"a\", \"b\"]", changes[0].OldValue);
        Assert.AreEqual("[\"b\", \"c\"]", changes[0].NewValue);
    }

    [TestMethod]
    public void Compare_EmptyStringAndEmptyArray_LikeMissing()
    {
        var before = new BsonDocument { { "Name", "" }, { "Stores", new BsonArray() } };

        Assert.AreEqual(0, Compare(before, new BsonDocument()).Count);
    }

    [TestMethod]
    public void Compare_ArrayOfDocsWithoutId_ByIndex()
    {
        BsonDocument Locale(string value) => new() { { "LanguageId", "l" }, { "LocaleValue", value } };
        var before = new BsonDocument("Locales", new BsonArray { Locale("a"), Locale("b") });
        var after = new BsonDocument("Locales", new BsonArray { Locale("a"), Locale("c") });

        var changes = Compare(before, after);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("Locales[1].LocaleValue", changes[0].Path);
    }

    private static BsonDocument UserField(string key, string value)
    {
        return new BsonDocument { { "Key", key }, { "Value", value }, { "StoreId", "" } };
    }

    [TestMethod]
    public void Compare_KeyValueArray_MatchesByKey()
    {
        var before = new BsonDocument("UserFields", new BsonArray { UserField("A", "1"), UserField("B", "x") });
        var after = new BsonDocument("UserFields", new BsonArray { UserField("B", "x"), UserField("A", "2") });

        var changes = Compare(before, after);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("UserFields[A].Value", changes[0].Path);
        Assert.AreEqual("1", changes[0].OldValue);
        Assert.AreEqual("2", changes[0].NewValue);
    }

    [TestMethod]
    public void Compare_KeyValueArray_AddedKey_ShowsValue()
    {
        var changes = Compare(new BsonDocument("UserFields", new BsonArray()),
            new BsonDocument("UserFields", new BsonArray { UserField("ImpersonatedCustomerId", "c9") }));

        Assert.AreEqual("c9", changes.Single(c => c.Path == "UserFields[ImpersonatedCustomerId].Value").NewValue);
    }

    [TestMethod]
    public void Compare_KeyValueArray_SensitiveKey_Masked()
    {
        var before = new BsonDocument("UserFields", new BsonArray { UserField("PasswordRecoveryToken", "old-secret") });
        var after = new BsonDocument("UserFields", new BsonArray {
            UserField("PasswordRecoveryToken", "new-secret"), UserField("TwoFactorSecretKey", "totp-secret")
        });

        var changes = Compare(before, after);

        //the changed token, and the Key and Value of the added secret
        Assert.AreEqual(3, changes.Count);
        foreach (var change in changes)
        {
            Assert.IsTrue(change.Masked, change.Path);
            Assert.IsNull(change.OldValue);
            Assert.IsNull(change.NewValue);
        }
    }

    [TestMethod]
    public void Compare_MaskedLeaf_ValuesHidden()
    {
        var before = new BsonDocument { { "Password", "old" }, { "Config", new BsonDocument("ApiKey", "k1") } };
        var after = new BsonDocument { { "Password", "new" }, { "Config", new BsonDocument("ApiKey", "k2") } };

        var changes = Compare(before, after);

        Assert.AreEqual(2, changes.Count);
        foreach (var change in changes)
        {
            Assert.IsTrue(change.Masked, change.Path);
            Assert.IsNull(change.OldValue);
            Assert.IsNull(change.NewValue);
        }
    }

    [TestMethod]
    public void Compare_LongValue_Truncated()
    {
        var changes = Compare(new BsonDocument("FullDescription", "short"),
            new BsonDocument("FullDescription", new string('x', 5000)));

        Assert.AreEqual(1024, changes[0].NewValue.Length);
        Assert.IsTrue(changes[0].Truncated);
    }

    [TestMethod]
    public void Compare_Binary_NotCompared_ListedAsChanged()
    {
        var changes = Compare(new BsonDocument("PictureBinary", new BsonBinaryData([1, 2, 3])),
            new BsonDocument("PictureBinary", new BsonBinaryData([1, 2, 3])));

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("<binary>", changes[0].OldValue);
        Assert.AreEqual("<binary>", changes[0].NewValue);
    }

    [TestMethod]
    public void Compare_SettingMetadata_DiffsInsideJson()
    {
        var changes = Compare(new BsonDocument("Metadata", "{\"RetentionDays\":365}"),
            new BsonDocument("Metadata", "{\"RetentionDays\":180}"), "Setting");

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("Metadata.RetentionDays", changes[0].Path);
        Assert.AreEqual("365", changes[0].OldValue);
        Assert.AreEqual("180", changes[0].NewValue);
    }

    [TestMethod]
    public void Compare_SettingMetadata_SecretMasked()
    {
        var changes = Compare(new BsonDocument("Metadata", "{\"ClientSecret\":\"x\"}"),
            new BsonDocument("Metadata", "{\"ClientSecret\":\"y\"}"), "Setting");

        Assert.AreEqual(1, changes.Count);
        Assert.IsTrue(changes[0].Masked);
        Assert.IsNull(changes[0].NewValue);
    }

    [TestMethod]
    public void Compare_SettingMetadata_InvalidJson_Masked()
    {
        var changes = Compare(new BsonDocument("Metadata", "{oops"), new BsonDocument("Metadata", "{}"), "Setting");

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("Metadata", changes[0].Path);
        Assert.IsTrue(changes[0].Masked);
    }

    [TestMethod]
    public void Compare_SettingMetadata_Null_DiffsInsideJson()
    {
        var changes = Compare(new BsonDocument("Metadata", BsonNull.Value),
            new BsonDocument("Metadata", "{\"RetentionDays\":5,\"ClientSecret\":\"abc\"}"), "Setting");

        Assert.AreEqual(2, changes.Count);
        Assert.AreEqual("5", changes.Single(c => c.Path == "Metadata.RetentionDays").NewValue);
        Assert.IsTrue(changes.Single(c => c.Path == "Metadata.ClientSecret").Masked);
    }

    [TestMethod]
    public void Compare_SettingMetadata_InvalidJson_ContentNeverStored()
    {
        var changes = Compare(new BsonDocument("Metadata", "{\"ClientSecret\":\"abc\""),
            new BsonDocument("Metadata", "{\"ClientSecret\":\"xyz\""), "Setting");

        Assert.AreEqual(1, changes.Count);
        Assert.IsFalse((changes[0].OldValue ?? "").Contains("abc"));
        Assert.IsFalse((changes[0].NewValue ?? "").Contains("xyz"));
    }

    [TestMethod]
    public void Compare_NullToDocument_NestedSecretMasked()
    {
        var changes = Compare(new BsonDocument("Config", BsonNull.Value),
            new BsonDocument("Config", new BsonDocument { { "Url", "u" }, { "ApiKey", "k" } }));

        Assert.IsTrue(changes.Single(c => c.Path == "Config.ApiKey").Masked);
        Assert.IsFalse(changes.Any(c => (c.NewValue ?? "").Contains("\"k\"")));
    }

    [TestMethod]
    public void Compare_ScalarToDocument_NestedSecretNotStored()
    {
        var changes = Compare(new BsonDocument("Config", "plain"),
            new BsonDocument("Config", new BsonDocument("ApiKey", "k")));

        Assert.IsTrue(changes.All(c => c.Masked || !(c.NewValue ?? "").Contains("ApiKey")));
    }

    [TestMethod]
    public void Compare_KeyValueWithoutValue_NestedSecretNotStored()
    {
        var changes = Compare(new BsonDocument("Pairs", new BsonArray()),
            new BsonDocument("Pairs", new BsonArray {
                new BsonDocument { { "Key", "Gateway" }, { "Data", new BsonDocument("ApiKey", "k") } }
            }));

        Assert.IsTrue(changes.Single(c => c.Path == "Pairs[Gateway].Data.ApiKey").Masked);
        Assert.IsFalse(changes.Any(c => c.NewValue == "k"));
    }

    [TestMethod]
    public void Compare_ArrayWithDuplicateIds_DoesNotThrow()
    {
        var before = new BsonDocument("Items", new BsonArray { TierPrice("a", 1), TierPrice("a", 2) });
        var after = new BsonDocument("Items", new BsonArray { TierPrice("a", 3) });

        Assert.IsNotNull(Compare(before, after));
    }

    [TestMethod]
    public void Compare_BeforeNull_InsertListsSetFields()
    {
        var changes = Compare(null, new BsonDocument { { "_id", "1" }, { "Name", "Shoes" }, { "Empty", BsonNull.Value } });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("Name", changes[0].Path);
        Assert.IsNull(changes[0].OldValue);
        Assert.AreEqual("Shoes", changes[0].NewValue);
    }

    [TestMethod]
    public void Compare_AfterNull_DeleteReturnsEmptyChanges()
    {
        Assert.AreEqual(0, Compare(new BsonDocument("Name", "Shoes"), null).Count);
    }

    [TestMethod]
    public void EntityName_PicksFirstPresent()
    {
        string Name(BsonDocument document) => AuditDiff.EntityName(AuditSnapshot.Flatten(document, false));

        Assert.AreEqual("Blue", Name(new BsonDocument { { "Email", "a@b.c" }, { "Name", "Blue" } }));
        Assert.AreEqual("a@b.c", Name(new BsonDocument { { "Name", "" }, { "Email", "a@b.c" } }));
        Assert.IsNull(Name(new BsonDocument("Price", 1)));
        Assert.IsNull(AuditDiff.EntityName(null));
    }
}
