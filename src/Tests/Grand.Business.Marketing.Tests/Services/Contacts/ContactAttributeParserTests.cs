using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Marketing.Services.Contacts;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Contacts;

[TestClass]
public class ContactAttributeParserTests
{
    private List<ContactAttribute> _contactAtr;
    private Mock<IContactAttributeService> _contactAttributeServiceMock;
    private ContactAttributeParser _parser;
    private Mock<IWorkContext> _workContextMock;
    private List<CustomAttribute> customAtr;

    [TestInitialize]
    public void Init()
    {
        _contactAttributeServiceMock = new Mock<IContactAttributeService>();
        _workContextMock = new Mock<IWorkContext>();
        var _translationServiceMock = new Mock<ITranslationService>();
        {
            _translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>())).Returns("Warning{0}");
        }

        _parser = new ContactAttributeParser(_contactAttributeServiceMock.Object, _workContextMock.Object);
        customAtr = [
            new CustomAttribute { Key = "key1", Value = "value1" },
            new CustomAttribute { Key = "key2", Value = "value2" },
            new CustomAttribute { Key = "key3", Value = "value3" },
            new CustomAttribute { Key = "key4", Value = "value4" }
        ];

        _contactAtr = [
            new ContactAttribute { Id = "key1", Name = "name1", AttributeControlType = AttributeControlType.TextBox },
            new ContactAttribute { Id = "key2", Name = "name2" },
            new ContactAttribute { Id = "key3", Name = "name3" },
            new ContactAttribute { Id = "key4", Name = "name4" },
            new ContactAttribute { Id = "key5", Name = "name5" }
        ];
        _contactAtr.ForEach(c =>
            c.ContactAttributeValues.Add(new ContactAttributeValue { Id = "value" + c.Id.Last() }));
    }

    [TestMethod]
    public async Task ParseContactAttributes_ReturnExpectedValues()
    {
        _contactAttributeServiceMock.Setup(c => c.GetContactAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_contactAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var result = await _parser.ParseContactAttributes(customAtr);
        Assert.IsTrue(result.Count == 4);
        Assert.IsTrue(result.Any(c => c.Id.Equals("key1")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("key2")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("key3")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("key4")));
        Assert.IsFalse(result.Any(c => c.Id.Equals("key5")));
    }

    [TestMethod]
    public async Task ParseCustomer0AttributeValues_ReturnExpectedValues()
    {
        _contactAttributeServiceMock.Setup(c => c.GetContactAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_contactAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var result = await _parser.ParseContactAttributeValues(customAtr);
        Assert.IsTrue(result.Count == 3);
        Assert.IsTrue(result.Any(c => c.Id.Equals("value2")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("value3")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("value4")));
        Assert.IsFalse(result.Any(c => c.Id.Equals("value5")));
    }

    [TestMethod]
    public void AddContactAttributeTest()
    {
        var result = _parser.AddContactAttribute(customAtr, new ContactAttribute { Id = "key7" }, "value7");
        Assert.IsTrue(result.Count == 5);
        Assert.IsTrue(result.Any(c => c.Key.Equals("key7")));
    }

    [TestMethod]
    public async Task IsConditionMetTest_IsConditionMet_True()
    {
        _contactAttributeServiceMock.Setup(c => c.GetContactAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_contactAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var contactAttribute = new ContactAttribute();
        contactAttribute.ConditionAttribute.Add(new CustomAttribute { Key = "key2", Value = "2" });
        var result = await _parser.IsConditionMet(contactAttribute,
            new List<CustomAttribute> { new() { Key = "key2", Value = "2" } });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsConditionMetTest_IsConditionMet_False()
    {
        _contactAttributeServiceMock.Setup(c => c.GetContactAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_contactAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var contactAttribute = new ContactAttribute();
        contactAttribute.ConditionAttribute.Add(new CustomAttribute { Key = "key2", Value = "2" });
        var result = await _parser.IsConditionMet(contactAttribute,
            new List<CustomAttribute> { new() { Key = "key3", Value = "2" } });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void RemoveContactAttributeTest()
    {
        var result = _parser.RemoveContactAttribute(customAtr, new ContactAttribute { Id = "key1" });
        Assert.IsTrue(result.Count == 3);
        Assert.IsFalse(result.Any(c => c.Key.Equals("key1")));
    }

    [TestMethod]
    public async Task FormatAttributesTest()
    {
        _contactAttributeServiceMock.Setup(c => c.GetAllContactAttributes("", false))
            .Returns(() => Task.FromResult((IList<ContactAttribute>)_contactAtr));
        _contactAttributeServiceMock.Setup(c => c.GetContactAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_contactAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var result = await _parser.FormatAttributes(new Language(),
            new List<CustomAttribute> {
                new() { Key = "key1", Value = "value1" }
            }, new Customer());
        Assert.IsTrue(result == "name1: value1");
    }
}