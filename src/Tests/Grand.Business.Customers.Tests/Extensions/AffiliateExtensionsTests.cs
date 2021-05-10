using Grand.Business.Customers.Extensions;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Affiliates;
using Grand.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Tests.Extensions
{
    [TestClass()]
    public class AffiliateExtensionsTests
    {
        private const string _fakeStoreUrl = "http://localhost/";
        private string _expectedFormat = _fakeStoreUrl+"?{0}={1}";
        private Mock<IWorkContext> _workContextMock;
        private Mock<IAffiliateService> _affiliateServiceMock;

        [TestInitialize()]
        public void TestInitialize()
        {

            _workContextMock = new Mock<IWorkContext>();
            _affiliateServiceMock = new Mock<IAffiliateService>();
        }


        [TestMethod()]
        public void GetFullName_NullParameter_ThrowException()
        {
            Affiliate affiliate = null;
            Assert.ThrowsException<ArgumentNullException>(() => affiliate.GetFullName());
        }

        [TestMethod()]
        public void GetFullName_NullLastName_ReturnFirstName()
        {
            var firstName = "Test name";
            Affiliate affiliate = new Affiliate
            {
                Address = new Domain.Common.Address() { FirstName = firstName }
            };
            Assert.AreEqual(firstName, affiliate.GetFullName());
        }

        [TestMethod()]
        public void GetFullName_NullFirstName_ReturnLastName()
        {
            var lastName = "Test name";
            Affiliate affiliate = new Affiliate
            {
                Address = new Domain.Common.Address() { LastName = lastName }
            };
            Assert.AreEqual(lastName, affiliate.GetFullName());
        }

        [TestMethod()]
        public void GetFullName_ValidParameter_ReturnLastName()
        {
            var fullName = string.Format("{0} {1}", "first name", "lastName");
            Affiliate affiliate = new Affiliate
            {
                Address = new Domain.Common.Address() { LastName = "lastName", FirstName = "first name" }
            };
            Assert.AreEqual(fullName, affiliate.GetFullName());
        }

        [TestMethod()]
        public void GenerateUrl_NullAffiliate_ThrowException()
        {
            Affiliate affiliate = null;
            Assert.ThrowsException<ArgumentNullException>(() => affiliate.GenerateUrl(null), "affiliate");
        }

        [TestMethod()]
        public void GenerateUrl_NullWebHelper_ThrowException()
        {
            Affiliate affiliate = new Affiliate();
            Assert.ThrowsException<ArgumentNullException>(() => affiliate.GenerateUrl(null), "webHelper");
        }

        [TestMethod()]
        public void GenerateUrl_NullFriendlyUrlName_UseId()
        {
            var id = "id";
            Affiliate affiliate = new Affiliate()
            {
                Id = id
            };
            _workContextMock.Setup(c => c.CurrentStore).Returns(new Domain.Stores.Store() { Url = _fakeStoreUrl});
            Assert.AreEqual(string.Format(_expectedFormat, "affiliateid", id), affiliate.GenerateUrl(_workContextMock.Object));
            //_webHelperMock.Verify(c => c.ModifyQueryString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod()]
        public void GenerateUrl_ValideParameters()
        {
            var friendlyUrl = "friendlyurl";
            Affiliate affiliate = new Affiliate()
            {
                FriendlyUrlName = friendlyUrl
            };
            _workContextMock.Setup(c => c.CurrentStore).Returns(new Domain.Stores.Store() { Url = _fakeStoreUrl });
            Assert.AreEqual(string.Format(_expectedFormat, "affiliate", friendlyUrl), affiliate.GenerateUrl(_workContextMock.Object));
        }

        [TestMethod()]
        public async Task ValidateFriendlyUrlName_UrlNameDontExist_ReturnUrl()
        {
            //Don't exist
            _affiliateServiceMock.Setup(c => c.GetAffiliateByFriendlyUrlName(It.IsAny<string>()))
              .Returns(() => Task.FromResult<Affiliate>(null));
            var friendlyUrl = "macbool-pro";
            Affiliate affiliate = new Affiliate()
            {
                FriendlyUrlName = friendlyUrl
            };
            var result = await affiliate.ValidateFriendlyUrlName(_affiliateServiceMock.Object, new Domain.Seo.SeoSettings(), friendlyUrl, "");
            Assert.AreEqual(result, friendlyUrl);
        }

        [TestMethod()]
        public async Task ValidateFriendlyUrlName_UrlNameExist_IncreaseUrl()
        {
            int invokeNumber = 1;
            _affiliateServiceMock.Setup(c => c.GetAffiliateByFriendlyUrlName(It.IsAny<string>()))
              .Returns(() =>
              {
                  if (invokeNumber <= 2) return Task.FromResult(new Affiliate());
                  else return Task.FromResult<Affiliate>(null);

              }).Callback(() => invokeNumber++);
            var friendlyUrl = "macbool-pro";
            var expectedUrl = "macbool-pro-3";
            Affiliate affiliate = new Affiliate()
            {
                FriendlyUrlName = friendlyUrl
            };
            var result = await affiliate.ValidateFriendlyUrlName(_affiliateServiceMock.Object, new Domain.Seo.SeoSettings(), friendlyUrl, "");
            Assert.AreEqual(result, expectedUrl);
        }
    }
}
