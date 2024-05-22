using Grand.Business.Cms.Services;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure.TypeConverters.Converter;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel;

namespace Grand.Business.Cms.Tests.Services;

[TestClass]
public class CookiePreferenceTests
{
    private CookiePreference _cookiePreferences;

    [TestInitialize]
    public void Init()
    {
        var cookie1 = new Mock<IConsentCookie>();
        var cookie2 = new Mock<IConsentCookie>();
        cookie1.Setup(c => c.DisplayOrder).Returns(1);
        cookie1.Setup(c => c.SystemName).Returns("cookie1");
        cookie2.Setup(c => c.DisplayOrder).Returns(2);
        cookie2.Setup(c => c.SystemName).Returns("cookie2");

        _cookiePreferences = new CookiePreference(new List<IConsentCookie> { cookie1.Object, cookie2.Object });
    }

    [TestMethod]
    public void GetConsentCookies_ReturnCorectOrder()
    {
        var result = _cookiePreferences.GetConsentCookies();
        Assert.IsTrue(result.First().DisplayOrder == 1);
        Assert.IsTrue(result.Last().DisplayOrder == 2);
    }

    [TestMethod]
    public async Task IsEnable_ReturnExpectedValue()
    {
        var dic = new Dictionary<string, bool>();
        dic.Add("cookie1", true);
        dic.Add("cookie2", false);

        TypeDescriptor.AddAttributes(typeof(Dictionary<string, bool>),
           new TypeConverterAttribute(typeof(GenericDictionaryTypeConverter<string, bool>)));

        var store = new Store();
        var valueStr = CommonHelper.To<string>(dic);
        var customer = new Customer();
        customer.UserFields.Add(new UserField(){ StoreId = store.Id, Key = SystemCustomerFieldNames.ConsentCookies, Value = valueStr});
        var result1 = await _cookiePreferences.IsEnable(customer, store, "cookie1");
        var result2 = await _cookiePreferences.IsEnable(customer, store, "cookie2");
        Assert.IsTrue(result1.Value);
        Assert.IsFalse(result2.Value);
    }
}