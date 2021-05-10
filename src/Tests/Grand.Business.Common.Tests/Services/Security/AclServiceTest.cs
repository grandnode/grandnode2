using Grand.Business.Common.Services.Security;
using Grand.Domain.Catalog;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Security
{
    [TestClass]
    public class AclServiceTest
    {
        private CatalogSettings _settings;
        private AclService _aclService;

        [TestInitialize]
        public void Init()
        {
            _settings = new CatalogSettings();
            _aclService = new AclService();
        }

        [TestMethod]
        public void Authorize_ReturnFalse()
        {
            Product product = null;
            Assert.IsFalse(_aclService.Authorize(product, "id"));
            product = new Product();
            product.LimitedToStores = true;
            Assert.IsFalse(_aclService.Authorize(product, "id"));
        }

        [TestMethod]
        public void Authorize_ReturnTrue()
        {
            Product  product = new Product();
            product.LimitedToStores = false;
            Assert.IsTrue(_aclService.Authorize(product, "id"));
            Assert.IsTrue(_aclService.Authorize(product, ""));
            CommonHelper.IgnoreStoreLimitations = true;
            Assert.IsTrue(_aclService.Authorize(product, "id"));
        }
    }
}
