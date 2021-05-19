using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Services.Directory;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Directory
{
    [TestClass()]
    public class CookiePreferenceTests
    {
        private Mock<IUserFieldService> _userFieldServiceMock;
        private CookiePreference _cookiePreferences;

        [TestInitialize()]
        public void Init()
        {
            var cookie1 = new Mock<IConsentCookie>();
            var cookie2 = new Mock<IConsentCookie>();
            cookie1.Setup(c => c.DisplayOrder).Returns(1);
            cookie1.Setup(c => c.SystemName).Returns("cookie1");
            cookie2.Setup(c => c.DisplayOrder).Returns(2);
            cookie2.Setup(c => c.SystemName).Returns("cookie2");

            _userFieldServiceMock = new Mock<IUserFieldService>();
            _cookiePreferences = new CookiePreference(_userFieldServiceMock.Object, new List<IConsentCookie>() { cookie1.Object, cookie2.Object });
        }

        [TestMethod()]
        public void GetConsentCookies_ReturnCorectOrder()
        {
            var result = _cookiePreferences.GetConsentCookies();
            Assert.IsTrue(result.First().DisplayOrder == 1);
            Assert.IsTrue(result.Last().DisplayOrder == 2);
        }

        [TestMethod()]
        public async Task IsEnable_ReturnExpectedValue()
        {
            var dic = new Dictionary<string, bool>();
            dic.Add("cookie1", true);
            dic.Add("cookie2", false);
            _userFieldServiceMock.Setup(c => c.GetFieldsForEntity<Dictionary<string, bool>>(It.IsAny<BaseEntity>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(dic));

            var result1 = await _cookiePreferences.IsEnable(new Customer(), new Store(), "cookie1");
            var result2 = await _cookiePreferences.IsEnable(new Customer(), new Store(), "cookie2");
            Assert.IsTrue(result1.Value);
            Assert.IsFalse(result2.Value);
        }
    }
}
