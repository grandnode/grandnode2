﻿using Grand.Business.Catalog.Tests.Services.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Discounts;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Tests.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Services.Discounts.Tests
{
    [TestClass()]
    public class DiscountServiceTests
    {
        private IRepository<Discount> _repository;
        private IRepository<DiscountCoupon> _discountCouponRepository;
        private IRepository<DiscountUsageHistory> _discountUsageHistoryRepository;
        private Mock<ITranslationService> _translationServiceMock;

        private Mock<IWorkContext> _workContextMock;
        private Mock<IMediator> _mediatorMock;
        private DiscountService _dicountService;
        private MemoryCacheBase _cacheBase;

        private IEnumerable<IDiscountProvider> _discountProviders;
        private IEnumerable<IDiscountAmountProvider> _discountAmountProviders;

        [TestInitialize()]
        public void InitializeTests()
        {
            CommonPath.BaseDirectory = "";

            _repository = new MongoDBRepositoryTest<Discount>();
            _discountCouponRepository = new MongoDBRepositoryTest<DiscountCoupon>();
            _discountUsageHistoryRepository = new MongoDBRepositoryTest<DiscountUsageHistory>();
            _workContextMock = new Mock<IWorkContext>();
            _translationServiceMock = new Mock<ITranslationService>();
            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store() { Id = "" });
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
            _mediatorMock = new Mock<IMediator>();
            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object);

            _discountProviders = new List<IDiscountProvider> { new DiscountProviderTest() };
            _discountAmountProviders = new List<IDiscountAmountProvider> { new DiscountAmountProviderTests() };

            _dicountService = new DiscountService(_cacheBase, _repository, _discountCouponRepository, _discountUsageHistoryRepository, _translationServiceMock.Object, 
                _workContextMock.Object, _discountProviders, _discountAmountProviders, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task GetDiscountByIdTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test"
            };
            await _dicountService.InsertDiscount(discount);

            //Act
            var result = await _dicountService.GetDiscountById(discount.Id);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test", result.Name);
        }

        [TestMethod()]
        public async Task GetAllDiscountsTest()
        {
            //Arrange
            var discount1 = new Discount() {
                Name = "test1",
                IsEnabled = true,
            };
            await _dicountService.InsertDiscount(discount1);
            
            var discount2 = new Discount() {
                Name = "test2",
                IsEnabled = true,
            };

            await _dicountService.InsertDiscount(discount2);

            //Act
            var result = await _dicountService.GetAllDiscounts(null, showHidden: true);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod()]
        public async Task InsertDiscountTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test"
            };
            //Act
            await _dicountService.InsertDiscount(discount);

            //Assert
            Assert.IsNotNull(_repository.Table.FirstOrDefault(x=>x.Name == "test"));
            Assert.AreEqual("test", _repository.Table.FirstOrDefault(x => x.Name == "test").Name);

        }

        [TestMethod()]
        public async Task UpdateDiscountTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test"
            };
            await _dicountService.InsertDiscount(discount);

            //Act
            discount.Name = "test2";
            await _dicountService.UpdateDiscount(discount);

            //Assert
            Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
            Assert.AreEqual("test2", _repository.Table.FirstOrDefault(x => x.Name == "test2").Name);
        }

        [TestMethod()]
        public async Task DeleteDiscountTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test"
            };
            await _dicountService.InsertDiscount(discount);

            //Act
            await _dicountService.DeleteDiscount(discount);

            //Assert
            Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test"));
        }

        [TestMethod()]
        public void LoadDiscountProviderBySystemNameTest_NotNull()
        {
            //Act
            var provider = _dicountService.LoadDiscountProviderByRuleSystemName("RuleSystemName");
            //Assert
            Assert.IsNotNull(provider);
        }
        [TestMethod()]
        public void LoadDiscountProviderBySystemNameTest_Null()
        {
            //Act
            var provider = _dicountService.LoadDiscountProviderByRuleSystemName("RuleSystemName2");
            //Assert
            Assert.IsNull(provider);
        }

        [TestMethod()]
        public void LoadAllDiscountProvidersTest()
        {
            //Act
            var providers = _dicountService.LoadAllDiscountProviders();
            //Assert
            Assert.AreEqual(1, providers.Count);
        }

        [TestMethod()]
        public async Task GetDiscountByCouponCodeTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);

            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);

            //Act
            var coupon = await _dicountService.GetDiscountByCouponCode("TEST123");

            //Assert
            Assert.IsNotNull(coupon);
            Assert.AreEqual(discount.Id, coupon.Id);
        }

        [TestMethod()]
        public async Task ExistsCodeInDiscountTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);

            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);

            //Act
            var coupon = await _dicountService.ExistsCodeInDiscount("TEST123", discount.Id, null);

            //Assert
            Assert.AreEqual(true, coupon);
        }

        [TestMethod()]
        public async Task GetAllCouponCodesByDiscountIdTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);

            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);
            var discountCoupon1 = new DiscountCoupon() {
                CouponCode = "TEST124",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon1);
            var discountCoupon2 = new DiscountCoupon() {
                CouponCode = "TEST125",
                DiscountId = "id",
            };
            await _discountCouponRepository.InsertAsync(discountCoupon2);
            //Act
            var coupon = await _dicountService.GetAllCouponCodesByDiscountId(discount.Id);
            //Assert
            Assert.AreEqual(2, coupon.Count);
        }

        [TestMethod()]
        public async Task GetDiscountCodeByIdTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);

            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);
            var discountCoupon1 = new DiscountCoupon() {
                CouponCode = "TEST124",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon1);
            var discountCoupon2 = new DiscountCoupon() {
                CouponCode = "TEST125",
                DiscountId = "id",
            };
            await _discountCouponRepository.InsertAsync(discountCoupon2);
            //Act
            var coupon = await _dicountService.GetDiscountCodeById(discountCoupon.Id);
            //Assert
            Assert.IsNotNull(coupon);
            Assert.AreEqual("TEST123", coupon.CouponCode);
        }

        [TestMethod()]
        public async Task GetDiscountCodeByCodeTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);

            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);
            var discountCoupon1 = new DiscountCoupon() {
                CouponCode = "TEST124",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon1);
            var discountCoupon2 = new DiscountCoupon() {
                CouponCode = "TEST125",
                DiscountId = "id",
            };
            await _discountCouponRepository.InsertAsync(discountCoupon2);
            //Act
            var coupon = await _dicountService.GetDiscountCodeByCode(discountCoupon.CouponCode);
            //Assert
            Assert.IsNotNull(coupon);
            Assert.AreEqual("TEST123", coupon.CouponCode);
        }

        [TestMethod()]
        public async Task DeleteDiscountCouponTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);

            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);
            var discountCoupon1 = new DiscountCoupon() {
                CouponCode = "TEST124",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon1);
            var discountCoupon2 = new DiscountCoupon() {
                CouponCode = "TEST125",
                DiscountId = "id",
            };
            await _discountCouponRepository.InsertAsync(discountCoupon2);

            //Act
            await _dicountService.DeleteDiscountCoupon(discountCoupon);
            //Assert
            Assert.IsNull(_discountCouponRepository.Table.FirstOrDefault(x=>x.Id == discountCoupon.Id));
        }

        [TestMethod()]
        public async Task InsertDiscountCouponTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);

            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
            };
            //Act
            await _discountCouponRepository.InsertAsync(discountCoupon);

            //Assert
            Assert.IsNotNull(_discountCouponRepository.Table.FirstOrDefault(x => x.Id == discountCoupon.Id));
        }

        [TestMethod()]
        public async Task DiscountCouponSetAsUsedTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);

            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);

            //Act
            await _dicountService.DiscountCouponSetAsUsed("TEST123", true);

            //Assert
            Assert.IsNotNull(_discountCouponRepository.Table.FirstOrDefault(x => x.Id == discountCoupon.Id));
            Assert.IsTrue(_discountCouponRepository.Table.FirstOrDefault(x => x.Id == discountCoupon.Id).Used);
        }

        [TestMethod()]
        public async Task CancelDiscountTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);

            var discountUsageHistory = new DiscountUsageHistory() { 
                CreatedOnUtc = DateTime.UtcNow,
                DiscountId = discount.Id,
                OrderId = "123",
                CouponCode = "TEST123"
            };
            await _discountUsageHistoryRepository.InsertAsync(discountUsageHistory);

            //Act
            await _dicountService.CancelDiscount("123");

            //Assert
            Assert.IsTrue(_discountUsageHistoryRepository.Table.FirstOrDefault(x=>x.Id == discountUsageHistory.Id).Canceled);
            Assert.IsFalse(_discountCouponRepository.Table.FirstOrDefault(x => x.Id == discountCoupon.Id).Used);
        }

        [TestMethod()]
        public async Task ValidateDiscountTest_Valid_CouponCodesToValidate()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true,
                CurrencyCode = "USD"
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);
            var customer = new Customer();
            customer.UserFields.Add(new Domain.Common.UserField() { Key = SystemCustomerFieldNames.DiscountCoupons, Value = "TEST123", StoreId = "" });
            //Act
            var result = await _dicountService.ValidateDiscount(discount, customer, new Domain.Directory.Currency() { CurrencyCode = "USD" });
            //Assert
            Assert.IsTrue(result.IsValid);
        }
        [TestMethod()]
        public async Task ValidateDiscountTest_InValid_CouponCodesToValidate()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true,
                CurrencyCode = "USD",
                RequiresCouponCode = true
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);
            var customer = new Customer();
            customer.UserFields.Add(new Domain.Common.UserField() { Key = SystemCustomerFieldNames.DiscountCoupons, Value = "TEST12", StoreId = "" });
            //Act
            var result = await _dicountService.ValidateDiscount(discount, customer, new Domain.Directory.Currency() { CurrencyCode = "USD" });
            //Assert
            Assert.IsFalse(result.IsValid);
        }
        [TestMethod()]
        public async Task ValidateDiscountTest_InValid_CurrencyCode()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true,
                CurrencyCode = "EUR",                
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);
            var customer = new Customer();
            customer.UserFields.Add(new Domain.Common.UserField() { Key = SystemCustomerFieldNames.DiscountCoupons, Value = "TEST12", StoreId = "" });
            //Act
            var result = await _dicountService.ValidateDiscount(discount, customer, new Domain.Directory.Currency() { CurrencyCode = "USD" });
            //Assert
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod()]
        public async Task ValidateDiscountTest_InValid_EndDateUtc()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true,
                CurrencyCode = "USD",
                EndDateUtc = DateTime.UtcNow.AddDays(-1),
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);
            var customer = new Customer();
            customer.UserFields.Add(new Domain.Common.UserField() { Key = SystemCustomerFieldNames.DiscountCoupons, Value = "TEST12", StoreId = "" });
            //Act
            var result = await _dicountService.ValidateDiscount(discount, customer, new Domain.Directory.Currency() { CurrencyCode = "USD" });
            //Assert
            Assert.IsFalse(result.IsValid);
        }
        [TestMethod()]
        public async Task ValidateDiscountTest_InValid_Disable()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = false,
                CurrencyCode = "USD",
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);
            var customer = new Customer();
            customer.UserFields.Add(new Domain.Common.UserField() { Key = SystemCustomerFieldNames.DiscountCoupons, Value = "TEST12", StoreId = "" });
            //Act
            var result = await _dicountService.ValidateDiscount(discount, customer, new Domain.Directory.Currency() { CurrencyCode = "USD" });
            //Assert
            Assert.IsFalse(result.IsValid);
        }
        [TestMethod()]
        public async Task GetDiscountUsageHistoryByIdTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);

            var discountUsageHistory = new DiscountUsageHistory() {
                CreatedOnUtc = DateTime.UtcNow,
                DiscountId = discount.Id,
                OrderId = "123",
                CouponCode = "TEST123"
            };
            await _discountUsageHistoryRepository.InsertAsync(discountUsageHistory);

            //Act
            await _dicountService.GetDiscountUsageHistoryById(discountUsageHistory.Id);

            //Assert
            Assert.IsNotNull(_discountUsageHistoryRepository.Table.FirstOrDefault(x => x.Id == discountUsageHistory.Id));
            
        }

        [TestMethod()]
        public async Task GetAllDiscountUsageHistoryTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);

            var discountUsageHistory = new DiscountUsageHistory() {
                CreatedOnUtc = DateTime.UtcNow,
                DiscountId = discount.Id,
                OrderId = "123",
                CouponCode = "TEST123"
            };
            await _discountUsageHistoryRepository.InsertAsync(discountUsageHistory);
            var discountUsageHistory2 = new DiscountUsageHistory() {
                CreatedOnUtc = DateTime.UtcNow,
                DiscountId = discount.Id,
                OrderId = "124",
                CouponCode = "TEST123"
            };
            await _discountUsageHistoryRepository.InsertAsync(discountUsageHistory2);
            //Act
            var usageHistory = await _dicountService.GetAllDiscountUsageHistory();

            //Assert
            Assert.AreEqual(2, usageHistory.Count());

        }

        [TestMethod()]
        public async Task InsertDiscountUsageHistoryTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);

            var discountUsageHistory = new DiscountUsageHistory() {
                CreatedOnUtc = DateTime.UtcNow,
                DiscountId = discount.Id,
                OrderId = "123",
                CouponCode = "TEST123"
            };
            await _discountUsageHistoryRepository.InsertAsync(discountUsageHistory);
            
            //Act
            var usageHistory = await _dicountService.GetDiscountUsageHistoryById(discountUsageHistory.Id);

            //Assert
            Assert.IsNotNull(usageHistory);
            Assert.AreEqual(discountUsageHistory.CouponCode, usageHistory.CouponCode);
        }

        [TestMethod()]
        public async Task UpdateDiscountUsageHistoryTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);

            var discountUsageHistory = new DiscountUsageHistory() {
                CreatedOnUtc = DateTime.UtcNow,
                DiscountId = discount.Id,
                OrderId = "123",
                CouponCode = "TEST123"
            };
            await _discountUsageHistoryRepository.InsertAsync(discountUsageHistory);

            //Act
            discountUsageHistory.OrderId = "12";
            await _discountUsageHistoryRepository.UpdateAsync(discountUsageHistory);
            var usageHistory = await _dicountService.GetDiscountUsageHistoryById(discountUsageHistory.Id);

            //Assert
            Assert.IsNotNull(usageHistory);
            Assert.AreEqual(discountUsageHistory.OrderId, usageHistory.OrderId);
        }

        [TestMethod()]
        public async Task DeleteDiscountUsageHistoryTest()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);
            var discountCoupon = new DiscountCoupon() {
                CouponCode = "TEST123",
                DiscountId = discount.Id,
                Used = true
            };
            await _discountCouponRepository.InsertAsync(discountCoupon);

            var discountUsageHistory = new DiscountUsageHistory() {
                CreatedOnUtc = DateTime.UtcNow,
                DiscountId = discount.Id,
                OrderId = "123",
                CouponCode = "TEST123"
            };
            await _discountUsageHistoryRepository.InsertAsync(discountUsageHistory);

            //Act            
            await _discountUsageHistoryRepository.DeleteAsync(discountUsageHistory);
            var usageHistory = await _dicountService.GetDiscountUsageHistoryById(discountUsageHistory.Id);

            //Assert
            Assert.IsNull(usageHistory);
        }

        [TestMethod()]
        public async Task GetDiscountAmountTest_global()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                DiscountAmount = 10,
                CurrencyCode = "USD",
                IsEnabled = true
            };
            await _dicountService.InsertDiscount(discount);

            //Act            
            var discountamount = await _dicountService.GetDiscountAmount(discount, 
                new Customer(), 
                new Domain.Directory.Currency() { CurrencyCode = "USD" },new Product(), 100);

            //Assert
            Assert.AreEqual(discount.DiscountAmount, discountamount);
        }
        public async Task GetDiscountAmountTest_AssignedToAllProducts()
        {
            //Arrange
            var discount = new Discount() {
                Name = "test",
                DiscountAmount = 10,
                CurrencyCode = "USD",
                IsEnabled = true,
                DiscountTypeId = DiscountType.AssignedToAllProducts
            };
            await _dicountService.InsertDiscount(discount);

            //Act            
            var discountamount = await _dicountService.GetDiscountAmount(discount,
                new Customer(),
                new Domain.Directory.Currency() { CurrencyCode = "USD" }, new Product(), 100);

            //Assert
            Assert.AreEqual(discount.DiscountAmount, discountamount);
        }
        [TestMethod()]
        public async Task GetPreferredDiscountTest()
        {
            //Arrange
            var discount1 = new Discount() {
                Name = "test",
                DiscountAmount = 10,
                CurrencyCode = "USD",
                IsEnabled = true,
                DiscountTypeId = DiscountType.AssignedToAllProducts
            };
            await _dicountService.InsertDiscount(discount1);
            var discount2 = new Discount() {
                Name = "test",
                DiscountAmount = 20,
                CurrencyCode = "USD",
                IsEnabled = true,
                DiscountTypeId = DiscountType.AssignedToAllProducts
            };
            await _dicountService.InsertDiscount(discount2);
            //Act            
            var discountamount = await _dicountService.GetPreferredDiscount(
                new List<ApplyDiscount> { 
                    new ApplyDiscount() { DiscountId = discount1.Id, IsCumulative = false },
                    new ApplyDiscount() { DiscountId = discount2.Id, IsCumulative = false }
                },
                new Customer(),
                new Domain.Directory.Currency() { CurrencyCode = "USD" }, new Product(), 100);

            //Assert
            Assert.AreEqual(discount2.DiscountAmount, discountamount.discountAmount);
        }

        [TestMethod()]
        public async Task GetPreferredDiscountTest_IsCumulative_True()
        {
            //Arrange
            var discount1 = new Discount() {
                Name = "test",
                DiscountAmount = 10,
                CurrencyCode = "USD",
                IsEnabled = true,
                DiscountTypeId = DiscountType.AssignedToAllProducts
            };
            await _dicountService.InsertDiscount(discount1);
            var discount2 = new Discount() {
                Name = "test",
                DiscountAmount = 20,
                CurrencyCode = "USD",
                IsEnabled = true,
                DiscountTypeId = DiscountType.AssignedToAllProducts
            };
            await _dicountService.InsertDiscount(discount2);
            //Act            
            var discountamount = await _dicountService.GetPreferredDiscount(
                new List<ApplyDiscount> {
                    new ApplyDiscount() { DiscountId = discount1.Id, IsCumulative = true },
                    new ApplyDiscount() { DiscountId = discount2.Id, IsCumulative = true }
                },
                new Customer(),
                new Domain.Directory.Currency() { CurrencyCode = "USD" }, new Product(), 100);

            //Assert
            Assert.AreEqual(discount1.DiscountAmount + discount2.DiscountAmount, discountamount.discountAmount);
        }

        [TestMethod()]
        public async Task GetDiscountAmountProviderTest()
        {
            //Arrange
            var discount1 = new Discount() {
                Name = "test",
                DiscountAmount = 10,
                CurrencyCode = "USD",
                IsEnabled = true,
                CalculateByPlugin = true,
                DiscountPluginName = "SampleDiscountAmountProvider",
                DiscountTypeId = DiscountType.AssignedToAllProducts
            };
            await _dicountService.InsertDiscount(discount1);
            //Act
            var amount = await _dicountService.GetDiscountAmountProvider(discount1, new Customer(), new Product(), 100);
            //Assert
            Assert.AreEqual(9, amount);
        }

        [TestMethod()]
        public void LoadDiscountAmountProviderBySystemNameTest()
        {
            //Act
            var discountProvider = _dicountService.LoadDiscountAmountProviderBySystemName("SampleDiscountAmountProvider");
            //Assert
            Assert.IsNotNull(discountProvider);
        }

        [TestMethod()]
        public void LoadDiscountAmountProvidersTest()
        {
            //Act
            var discountProviders = _dicountService.LoadDiscountAmountProviders();
            //Assert
            Assert.AreEqual(1, discountProviders.Count);
        }
    }
}