using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Customers.Services;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services;

[TestClass]
public class CustomerAttributeParserTests
{
    private List<CustomerAttribute> _customerAtr;
    private Mock<ICustomerAttributeService> _customerAtrServiceMock;
    private CustomerAttributeParser _parser;
    private List<CustomAttribute> customAtr;

    [TestInitialize]
    public void Init()
    {
        _customerAtrServiceMock = new Mock<ICustomerAttributeService>();
        var _translationServiceMock = new Mock<ITranslationService>();
        {
            _translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>())).Returns("Warning{0}");
        }

        _parser = new CustomerAttributeParser(_customerAtrServiceMock.Object);
        customAtr = [
            new CustomAttribute { Key = "key1", Value = "value1" },
            new CustomAttribute { Key = "key2", Value = "value2" },
            new CustomAttribute { Key = "key3", Value = "value3" },
            new CustomAttribute { Key = "key4", Value = "value4" }
        ];

        _customerAtr = [
            new CustomerAttribute {
                Id = "key1", Name = "name1", AttributeControlTypeId = AttributeControlType.TextBox
            },
            new CustomerAttribute { Id = "key2", Name = "name2" },
            new CustomerAttribute { Id = "key3", Name = "name3" },
            new CustomerAttribute { Id = "key4", Name = "name4" },
            new CustomerAttribute { Id = "key5", Name = "name5" }
        ];
        _customerAtr.ForEach(c =>
            c.CustomerAttributeValues.Add(new CustomerAttributeValue { Id = "value" + c.Id.Last() }));
    }

    [TestMethod]
    public async Task ParseCustomerAttributes_ReturnExpectedValues()
    {
        _customerAtrServiceMock.Setup(c => c.GetCustomerAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_customerAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var result = await _parser.ParseCustomerAttributes(customAtr);
        Assert.IsTrue(result.Count == 4);
        Assert.IsTrue(result.Any(c => c.Id.Equals("key1")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("key2")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("key3")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("key4")));
        Assert.IsFalse(result.Any(c => c.Id.Equals("key5")));
    }

    [TestMethod]
    public async Task ParseCustomerAttributes_ReturnEmptyList()
    {
        //not exist
        _customerAtrServiceMock.Setup(c => c.GetCustomerAttributeById(It.IsAny<string>()))
            .Returns(() => Task.FromResult<CustomerAttribute>(null));
        var result = await _parser.ParseCustomerAttributes(customAtr);
        Assert.IsTrue(result.Count == 0);
    }

    [TestMethod]
    public async Task ParseCustomer0AttributeValues_ReturnExpectedValues()
    {
        _customerAtrServiceMock.Setup(c => c.GetCustomerAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_customerAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var result = await _parser.ParseCustomerAttributeValues(customAtr);
        Assert.IsTrue(result.Count == 3);
        Assert.IsTrue(result.Any(c => c.Id.Equals("value2")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("value3")));
        Assert.IsTrue(result.Any(c => c.Id.Equals("value4")));
        Assert.IsFalse(result.Any(c => c.Id.Equals("value5")));
    }


    [TestMethod]
    public void AddCustomerAttribute_ReturnExpectedValues()
    {
        //not exist

        var result = _parser.AddCustomerAttribute(customAtr, new CustomerAttribute { Id = "key7" }, "value7");
        Assert.IsTrue(result.Count == 5);
        Assert.IsTrue(result.Any(c => c.Key.Equals("key7")));
    }

    [TestMethod]
    public async Task GetAttributeWarnings_Return_NoWarning()
    {
        _customerAtrServiceMock.Setup(c => c.GetAllCustomerAttributes())
            .Returns(() => Task.FromResult((IList<CustomerAttribute>)_customerAtr));
        _customerAtrServiceMock.Setup(c => c.GetCustomerAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_customerAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var result = await _parser.GetAttributeWarnings(customAtr);
        Assert.IsTrue(result.Count == 0);
    }

    [TestMethod]
    public async Task GetAttributeWarnings_Return_WithWarning()
    {
        _customerAtr.FirstOrDefault(x => x.Id == "key5").IsRequired = true;
        _customerAtrServiceMock.Setup(c => c.GetAllCustomerAttributes())
            .Returns(() => Task.FromResult((IList<CustomerAttribute>)_customerAtr));
        _customerAtrServiceMock.Setup(c => c.GetCustomerAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_customerAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var result = await _parser.GetAttributeWarnings(customAtr);
        Assert.IsTrue(result.Count == 1);
    }

    [TestMethod]
    public async Task FormatAttributes_Return_string()
    {
        _customerAtrServiceMock.Setup(c => c.GetAllCustomerAttributes())
            .Returns(() => Task.FromResult((IList<CustomerAttribute>)_customerAtr));
        _customerAtrServiceMock.Setup(c => c.GetCustomerAttributeById(It.IsAny<string>())).Returns((string w) =>
            Task.FromResult(_customerAtr.FirstOrDefault(a => a.Id.Equals(w))));
        var result = await _parser.FormatAttributes(new Language(),
            new List<CustomAttribute> {
                new() { Key = "key1", Value = "value1" }
            });
        Assert.IsTrue(result == "name1: value1");
    }
}