using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Captcha;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Customer;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.Customers;

/// <summary>
///     Verifies the storefront customer validators thread the current store into the customer lookup when
///     per-store customer identity is enabled (Customer:RegisterCustomersPerStore).
/// </summary>
[TestClass]
public class CustomerValidatorsStoreScopingTests
{
    private const string StoreId = "store-1";

    private static Mock<ITranslationService> Translation()
    {
        var m = new Mock<ITranslationService>();
        m.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);
        return m;
    }

    private static IContextAccessor Context(string currentCustomerEmail = "owner@x.com")
    {
        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(new Customer { Email = currentCustomerEmail });
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = StoreId });
        var ctx = new Mock<IContextAccessor>();
        ctx.Setup(c => c.WorkContext).Returns(work.Object);
        ctx.Setup(c => c.StoreContext).Returns(store.Object);
        return ctx.Object;
    }

    private static CustomerConfigProbe PerStoreOn() => new(true);

    //small wrapper so the intent (per-store on) reads clearly at call sites
    private sealed record CustomerConfigProbe(bool On)
    {
        public Grand.Infrastructure.Configuration.CustomerConfig Config => new() { RegisterCustomersPerStore = On };
    }

    [TestMethod]
    public async Task LoginValidator_ScopesEmailLookupToCurrentStore()
    {
        var customerService = new Mock<ICustomerService>();
        var validator = new LoginValidator(
            new List<IValidatorConsumer<LoginModel>>(),
            new List<IValidatorConsumer<ICaptchaValidModel>>(),
            customerService.Object, new Mock<IGroupService>().Object, new Mock<IEncryptionService>().Object,
            Translation().Object, new CustomerSettings(), new Grand.Domain.Common.CaptchaSettings(),
            new Mock<IHttpContextAccessor>().Object, new Mock<IGoogleReCaptchaValidator>().Object,
            Context(), PerStoreOn().Config);

        await validator.ValidateAsync(new LoginModel { Email = "u@x.com", Password = "p" });

        customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task RegisterValidator_ScopesEmailLookupToCurrentStore()
    {
        var customerService = new Mock<ICustomerService>();
        var groupService = new Mock<IGroupService>();
        groupService.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(false);
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetParseCustomAttributes>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomAttribute>());
        var attrParser = new Mock<ICustomerAttributeParser>();
        attrParser.Setup(p => p.GetAttributeWarnings(It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        var validator = new RegisterValidator(
            new List<IValidatorConsumer<RegisterModel>>(),
            new List<IValidatorConsumer<ICaptchaValidModel>>(),
            Translation().Object, new Mock<ICountryService>().Object, new CustomerSettings(),
            new Grand.Domain.Common.CaptchaSettings(), new Mock<IHttpContextAccessor>().Object,
            new Mock<IGoogleReCaptchaValidator>().Object, mediator.Object, attrParser.Object,
            customerService.Object, groupService.Object, Context(), PerStoreOn().Config);

        await validator.ValidateAsync(new RegisterModel { Email = "u@x.com" });

        customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task PasswordRecoveryValidator_ScopesEmailLookupToCurrentStore()
    {
        var customerService = new Mock<ICustomerService>();
        var validator = new PasswordRecoveryValidator(
            new List<IValidatorConsumer<PasswordRecoveryModel>>(),
            new List<IValidatorConsumer<ICaptchaValidModel>>(),
            customerService.Object, new Grand.Domain.Common.CaptchaSettings(),
            new Mock<IHttpContextAccessor>().Object, new Mock<IGoogleReCaptchaValidator>().Object,
            Translation().Object, Context(), PerStoreOn().Config);

        await validator.ValidateAsync(new PasswordRecoveryModel { Email = "u@x.com" });

        customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task PasswordRecoveryConfirmValidator_ScopesEmailLookupToCurrentStore()
    {
        var customerService = new Mock<ICustomerService>();
        var validator = new PasswordRecoveryConfirmValidator(
            new List<IValidatorConsumer<PasswordRecoveryConfirmModel>>(),
            customerService.Object, new Mock<IGroupService>().Object,
            new Mock<ICustomerManagerService>().Object, new Mock<ICustomerHistoryPasswordService>().Object,
            Translation().Object, new CustomerSettings(), Context(), PerStoreOn().Config);

        await validator.ValidateAsync(new PasswordRecoveryConfirmModel {
            Email = "u@x.com", NewPassword = "abc", ConfirmNewPassword = "abc"
        });

        customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task CustomerInfoValidator_ScopesEmailLookupToCurrentStore_WhenEmailChanged()
    {
        var customerService = new Mock<ICustomerService>();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetParseCustomAttributes>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomAttribute>());
        var attrParser = new Mock<ICustomerAttributeParser>();
        attrParser.Setup(p => p.GetAttributeWarnings(It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        var validator = new CustomerInfoValidator(
            new List<IValidatorConsumer<CustomerInfoModel>>(),
            Context("old@x.com"), customerService.Object, mediator.Object, attrParser.Object,
            Translation().Object, new Mock<ICountryService>().Object,
            new CustomerSettings { AllowUsersToChangeEmail = true }, PerStoreOn().Config);

        await validator.ValidateAsync(new CustomerInfoModel { Email = "new@x.com" });

        customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task SubAccountCreateValidator_ScopesEmailLookupToCurrentStore()
    {
        var customerService = new Mock<ICustomerService>();
        var validator = new SubAccountCreateValidator(
            new List<IValidatorConsumer<SubAccountCreateModel>>(),
            customerService.Object, Translation().Object, new CustomerSettings(),
            Context(), PerStoreOn().Config);

        await validator.ValidateAsync(new SubAccountCreateModel {
            Email = "u@x.com", FirstName = "A", LastName = "B", Password = "p"
        });

        customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task SubAccountEditValidator_ScopesEmailLookupToCurrentStore_WhenEmailChanged()
    {
        var customerService = new Mock<ICustomerService>();
        customerService.Setup(c => c.GetCustomerById("c1"))
            .ReturnsAsync(new Customer { Id = "c1", Email = "old@x.com" });
        var groupService = new Mock<IGroupService>();
        groupService.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);

        var validator = new SubAccountEditValidator(
            new List<IValidatorConsumer<SubAccountEditModel>>(),
            customerService.Object, groupService.Object, Translation().Object,
            Context(), new CustomerSettings { AllowUsersToChangeEmail = true }, PerStoreOn().Config);

        await validator.ValidateAsync(new SubAccountEditModel {
            Id = "c1", Email = "new@x.com", FirstName = "A", LastName = "B"
        });

        customerService.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.AtLeastOnce);
    }
}
