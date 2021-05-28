using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Services.Tax;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Tax;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Domain.Catalog;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Catalog.Tests.Service.Tax
{
    [TestClass()]
    public class TaxServiceTests
    {

        private IWorkContext _workContext;
        private TaxSettings _taxSettings;
        private TaxProviderSettings _taxProviderSettings;
        private ITaxService _taxService;
        private IVatService _vatService;
        private IGeoLookupService _geoLookupService;
        private ICountryService _countryService;
        private CustomerSettings _customerSettings;
        private AddressSettings _addressSettings;
        private Mock<IGroupService> _groupServiceMock;
        private Mock<ILogger> _loggerMock;

        private IServiceProvider _serviceProvider;
        [TestInitialize()]
        public void TestInitialize()
        {
            CommonPath.BaseDirectory = "";

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(FixedRateTestTaxProvider))).Returns(new FixedRateTestTaxProvider());
            _serviceProvider = serviceProvider.Object;

            _taxProviderSettings = new TaxProviderSettings();
            _taxProviderSettings.ActiveTaxProviderSystemName = "test-provider";

            _taxSettings = new TaxSettings();
            _workContext = null;
            _geoLookupService = new Mock<IGeoLookupService>().Object;
            _countryService = new Mock<ICountryService>().Object;
            _customerSettings = new CustomerSettings();
            _addressSettings = new AddressSettings();
            _groupServiceMock = new Mock<IGroupService>();
            _loggerMock = new Mock<ILogger>();
            var providers = new List<ITaxProvider>();
            providers.Add(new FixedRateTestTaxProvider());
            _taxService = new TaxService( _workContext,_groupServiceMock.Object,
                 _geoLookupService, _countryService,providers ,_loggerMock.Object,
                _taxSettings, _taxProviderSettings, _customerSettings, _addressSettings);

            _vatService = new VatService(_taxSettings);
        }

        [TestMethod()]
        public async Task Can_check_taxExempt_product()
        {

            var product = new Product();
            product.IsTaxExempt = true;
            Assert.IsTrue(await _taxService.IsTaxExempt(product, null));
            product.IsTaxExempt = false;
            Assert.IsFalse(await _taxService.IsTaxExempt(product, null));
        }

        [TestMethod()]
        public async Task Can_check_taxExempt_customer()
        {

            var customer = new Customer();
            customer.Groups.Add("group sample id");
            customer.IsTaxExempt = true;
            Assert.IsTrue(await _taxService.IsTaxExempt(null, customer));
            _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>())).Returns(Task.FromResult<IList<CustomerGroup>>(new List<CustomerGroup>()));
            customer.IsTaxExempt = false;
            Assert.IsFalse(await _taxService.IsTaxExempt(null, customer));
        }
        /*
        [TestMethod()]
        public async Task Can_check_taxExempt_customer_in_taxExemptCustomerRole()
        {
            var customer = new Customer();
            customer.IsTaxExempt = false;
            Assert.IsFalse(await _taxService.IsTaxExempt(null, customer));

            var customerRole = new CustomerRole
            {
                TaxExempt = true,
                Active = true
            };
            customer.CustomerRoles.Add(customerRole);
            Assert.IsTrue(await _taxService.IsTaxExempt(null, customer));
            customerRole.TaxExempt = false;
            Assert.IsFalse(await _taxService.IsTaxExempt(null, customer));

            customerRole.Active = false;
            Assert.IsFalse(await _taxService.IsTaxExempt(null, customer));
        }
        */
        [TestMethod()]
        public async Task Can_get_productPrice_priceIncludesTax_includingTax_taxable()
        {
            var customer = new Customer();
            var product = new Product();
            _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>())).Returns(Task.FromResult<IList<CustomerGroup>>(new List<CustomerGroup>()));
            var pp = await _taxService.GetProductPrice(product, "0", 1000, true, customer, true);
            var price = pp.productprice;
            Assert.AreEqual(1000, price);
        }

        [TestMethod()]
        public async Task Can_get_productPrice_priceIncludesTax_includingTax_non_taxable()
        {

            var customer = new Customer { IsTaxExempt = true }; //not taxable
            var product = new Product();

            Assert.AreEqual(Math.Round(909.0909090, 2), (await _taxService.GetProductPrice(product, "0", 1000, true, customer, true)).productprice);
            Assert.AreEqual(1000, (await _taxService.GetProductPrice(product, "0", 1000, true, customer, false)).productprice);
            Assert.AreEqual(Math.Round(909.0909090, 2), (await _taxService.GetProductPrice(product, "0", 1000, false, customer, true)).productprice);
            Assert.AreEqual(1000, (await _taxService.GetProductPrice(product, "0", 1000, false, customer, false)).productprice);
        }

        [TestMethod()]
        public async Task Should_assume_valid_VAT_number_if_EuVatAssumeValid_setting_is_true()
        {
            _taxSettings.EuVatAssumeValid = true;
            VatNumberStatus vatNumberStatus = (await _vatService.GetVatNumberStatus("GB", "000 0000 00")).status;
            Assert.AreEqual(VatNumberStatus.Valid, vatNumberStatus);
        }

        [TestMethod()]
        public async Task GetProductPriceQuickly_NonTaxExemptAndPriceIncludingTax_ShouldReturnTheSameValues()
        {
            var product = new Product();
            product.IsTaxExempt = false;
            var customer = new Customer();
            customer.IsTaxExempt = false;

            string taxCategoryId = "";  //as in code
            double scUnitPriceWithoutDiscount = 1000.00;
            double scUnitPrice = 1000.00;
            double scSubTotal = 5000.00;
            double discountAmount = 7000.00;
            _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>())).Returns(Task.FromResult<IList<CustomerGroup>>(new List<CustomerGroup>()));
            //these 6 methods..
            var scUnitPriceInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scUnitPrice, true, customer, true)).productprice;
            var scUnitPriceExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scUnitPrice, false, customer, true)).productprice;
            var scSubTotalInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scSubTotal, true, customer, true)).productprice;
            var scSubTotalExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scSubTotal, false, customer, true)).productprice;
            var discountAmountInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, discountAmount, true, customer, true)).productprice;
            var discountAmountExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, discountAmount, false, customer, true)).productprice;
            //..should return the same value as this one method's properties are having
            var result02 = (await _taxService.GetTaxProductPrice(product, customer, scUnitPrice, scUnitPriceWithoutDiscount, 1, scSubTotal, discountAmount, true));

            Assert.AreEqual(scUnitPriceInclTax, result02.UnitPriceInclTax, "unit price including tax");
            Assert.AreEqual(scUnitPriceExclTax, result02.UnitPriceExclTax, "unit price excluding tax");
            Assert.AreEqual(scSubTotalInclTax, result02.SubTotalInclTax, "sub total including tax");
            Assert.AreEqual(scSubTotalExclTax, result02.SubTotalExclTax, "sub total excluding tax");
            Assert.AreEqual(discountAmountInclTax, result02.discountAmountInclTax, "discount including tax");
            Assert.AreEqual(discountAmountExclTax, result02.discountAmountExclTax, "discount excluding tax");
        }

        [TestMethod()]
        public async Task GetProductPriceQuickly_NonTaxExemptAndPriceExcludingTax_ShouldReturnTheSameValues()
        {
            var product = new Product();
            product.TaxCategoryId = "";
            product.IsTele = false;
            product.IsTaxExempt = false;
            var customer = new Customer();
            customer.IsTaxExempt = false;
            string taxCategoryId = "";  //as in code
            _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>())).Returns(Task.FromResult<IList<CustomerGroup>>(new List<CustomerGroup>()));
            double scUnitPriceWithoutDiscount = 1000.00;
            double scUnitPrice = 1000.00;
            double scSubTotal = 5000.00;
            double discountAmount = 7000.00;

            var scUnitPriceInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scUnitPrice, true, customer, false)).productprice;
            var scUnitPriceExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scUnitPrice, false, customer, false)).productprice;
            var scSubTotalInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scSubTotal, true, customer, false)).productprice;
            var scSubTotalExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scSubTotal, false, customer, false)).productprice;
            var discountAmountInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, discountAmount, true, customer, false)).productprice;
            var discountAmountExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, discountAmount, false, customer, false)).productprice;

            var result02 = (await _taxService.GetTaxProductPrice(product, customer, scUnitPrice, scUnitPriceWithoutDiscount, 1, scSubTotal, discountAmount, false));

            Assert.AreEqual(scUnitPriceInclTax, result02.UnitPriceInclTax, "unit price including tax");
            Assert.AreEqual(scUnitPriceExclTax, result02.UnitPriceExclTax, "unit price excluding tax");
            Assert.AreEqual(scSubTotalInclTax, result02.SubTotalInclTax, "sub total including tax");
            Assert.AreEqual(scSubTotalExclTax, result02.SubTotalExclTax, "sub total excluding tax");
            Assert.AreEqual(discountAmountInclTax, result02.discountAmountInclTax, "discount including tax");
            Assert.AreEqual(discountAmountExclTax, result02.discountAmountExclTax, "discount excluding tax");
        }

        [TestMethod()]
        public async Task GetProductPriceQuickly_TaxExemptAndPriceIncludingTax_ShouldReturnTheSameValues()
        {
            var product = new Product();
            product.TaxCategoryId = "57516fc81b0dc92b20fdd2ef";
            product.IsTele = false;
            product.IsTaxExempt = true;
            var customer = new Customer();
            customer.IsTaxExempt = true;
            string taxCategoryId = "";

            double scUnitPriceWithoutDiscount = 1000.00;
            double scUnitPrice = 1000.00;
            double scSubTotal = 5000.00;
            double discountAmount = 7000.00;

            var scUnitPriceInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scUnitPrice, true, customer, true)).productprice;
            var scUnitPriceExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scUnitPrice, false, customer, true)).productprice;
            var scSubTotalInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scSubTotal, true, customer, true)).productprice;
            var scSubTotalExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scSubTotal, false, customer, true)).productprice;
            var discountAmountInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, discountAmount, true, customer, true)).productprice;
            var discountAmountExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, discountAmount, false, customer, true)).productprice;

            var result02 = (await _taxService.GetTaxProductPrice(product, customer, scUnitPrice, scUnitPriceWithoutDiscount, 1, scSubTotal, discountAmount, true));

            Assert.AreEqual(scUnitPriceInclTax, result02.UnitPriceInclTax, "unit price including tax");
            Assert.AreEqual(scUnitPriceExclTax, result02.UnitPriceExclTax, "unit price excluding tax");
            Assert.AreEqual(scSubTotalInclTax, result02.SubTotalInclTax, "sub total including tax");
            Assert.AreEqual(scSubTotalExclTax, result02.SubTotalExclTax, "sub total excluding tax");
            Assert.AreEqual(discountAmountInclTax, result02.discountAmountInclTax, "discount including tax");
            Assert.AreEqual(discountAmountExclTax, result02.discountAmountExclTax, "discount excluding tax");
        }

        [TestMethod()]
        public async Task GetProductPriceQuickly_TaxExemptAndPriceExcludingTax_ShouldReturnTheSameValues()
        {
            var product = new Product();
            product.TaxCategoryId = "57516fc81b0dc92b20fdd2ef";
            product.IsTele = false;
            product.IsTaxExempt = true;
            var customer = new Customer();
            customer.IsTaxExempt = true;
            string taxCategoryId = "";

            double scUnitPriceWithoutDiscount = 1000.00;
            double scUnitPrice = 1000.00;
            double scSubTotal = 5000.00;
            double discountAmount = 7000.00;

            var scUnitPriceInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scUnitPrice, true, customer, false)).productprice;
            var scUnitPriceExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scUnitPrice, false, customer, false)).productprice;
            var scSubTotalInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scSubTotal, true, customer, false)).productprice;
            var scSubTotalExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, scSubTotal, false, customer, false)).productprice;
            var discountAmountInclTax = (await _taxService.GetProductPrice(product, taxCategoryId, discountAmount, true, customer, false)).productprice;
            var discountAmountExclTax = (await _taxService.GetProductPrice(product, taxCategoryId, discountAmount, false, customer, false)).productprice;

            var result02 = await (_taxService.GetTaxProductPrice(product, customer, scUnitPrice, scUnitPriceWithoutDiscount, 1, scSubTotal, discountAmount, false));

            Assert.AreEqual(scUnitPriceInclTax, result02.UnitPriceInclTax, "unit price including tax");
            Assert.AreEqual(scUnitPriceExclTax, result02.UnitPriceExclTax, "unit price excluding tax");
            Assert.AreEqual(scSubTotalInclTax, result02.SubTotalInclTax, "sub total including tax");
            Assert.AreEqual(scSubTotalExclTax, result02.SubTotalExclTax, "sub total excluding tax");
            Assert.AreEqual(discountAmountInclTax, result02.discountAmountInclTax, "discount including tax");
            Assert.AreEqual(discountAmountExclTax, result02.discountAmountExclTax, "discount excluding tax");
        }

    }
}
