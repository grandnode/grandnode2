using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class PanelPasswordRecoveryServiceTests
{
    private const string Email = "owner@example.com";
    private const string Token = "8c6a2f4e-6b0e-4b8c-9f1e-2d5c7a9b1e3f";

    private Mock<ICustomerService> _customerService;
    private Mock<ICustomerManagerService> _customerManagerService;
    private Mock<ICustomerHistoryPasswordService> _historyService;
    private Mock<IGroupService> _groupService;
    private Mock<IPermissionService> _permissionService;
    private Mock<IVendorService> _vendorService;
    private Mock<IMessageProviderService> _messageProviderService;
    private CustomerSettings _customerSettings;
    private Customer _customer;
    private PanelPasswordRecoveryService _service;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "c1", Email = Email, Active = true };
        _customerService = new Mock<ICustomerService>();
        _customerService.Setup(x => x.GetCustomerByEmail(Email, "")).ReturnsAsync(_customer);
        _customerManagerService = new Mock<ICustomerManagerService>();
        _historyService = new Mock<ICustomerHistoryPasswordService>();
        _groupService = new Mock<IGroupService>();
        _groupService.Setup(x => x.IsRegistered(_customer)).ReturnsAsync(true);
        _permissionService = new Mock<IPermissionService>();
        _vendorService = new Mock<IVendorService>();
        _messageProviderService = new Mock<IMessageProviderService>();

        var translationService = new Mock<ITranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>())).Returns((string key) => key);

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "lang" });
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Store { Id = "store" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);

        _customerSettings = new CustomerSettings { PasswordRecoveryLinkDaysValid = 7 };

        _service = new PanelPasswordRecoveryService(_customerService.Object, _customerManagerService.Object,
            _historyService.Object, _groupService.Object, _permissionService.Object, _vendorService.Object,
            _messageProviderService.Object, translationService.Object, contextAccessor.Object, _customerSettings);
    }

    private void GrantAdminPanel()
    {
        _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, _customer))
            .ReturnsAsync(true);
    }

    private void GrantStorePanel()
    {
        _customer.StaffStoreId = "store";
        _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessStoreManagerPanel, _customer))
            .ReturnsAsync(true);
        _groupService.Setup(x => x.IsStoreManager(_customer)).ReturnsAsync(true);
    }

    private void GrantVendorPanel(bool vendorActive = true)
    {
        _customer.VendorId = "v1";
        _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessVendorPanel, _customer))
            .ReturnsAsync(true);
        _groupService.Setup(x => x.IsVendor(_customer)).ReturnsAsync(true);
        _vendorService.Setup(x => x.GetVendorById("v1")).ReturnsAsync(new Vendor { Id = "v1", Active = vendorActive });
    }

    private void GiveToken(DateTime generatedUtc)
    {
        _customer.UserFields.Add(new UserField
            { Key = SystemCustomerFieldNames.PasswordRecoveryToken, Value = Token, StoreId = "" });
        _customer.UserFields.Add(new UserField {
            Key = SystemCustomerFieldNames.PasswordRecoveryTokenDateGenerated,
            Value = generatedUtc.ToString("o"), StoreId = ""
        });
    }

    private void VerifyMessageSent(Times times)
    {
        _messageProviderService.Verify(x => x.SendCustomerPasswordRecoveryMessage(
            It.IsAny<Customer>(), It.IsAny<Store>(), It.IsAny<string>()), times);
    }

    [TestMethod]
    public async Task SendRecoveryMessage_AdminOfAdminPanel_StoresTokenAndArea_SendsMessage()
    {
        GrantAdminPanel();

        await _service.SendRecoveryMessage(Email, "Admin");

        _customerService.Verify(x => x.UpdateUserField(_customer, SystemCustomerFieldNames.PasswordRecoveryToken,
            It.Is<string>(t => t.Length == 36), ""), Times.Once);
        _customerService.Verify(x => x.UpdateUserField(_customer, SystemCustomerFieldNames.PasswordRecoveryArea,
            "admin", ""), Times.Once);
        _messageProviderService.Verify(x => x.SendCustomerPasswordRecoveryMessage(_customer,
            It.Is<Store>(s => s.Id == "store"), "lang"), Times.Once);
    }

    [TestMethod]
    public async Task SendRecoveryMessage_UnknownEmail_SendsNothing()
    {
        await _service.SendRecoveryMessage("nobody@example.com", "Admin");

        VerifyMessageSent(Times.Never());
        _customerService.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task SendRecoveryMessage_CustomerWithoutPanelAccess_SendsNothing()
    {
        await _service.SendRecoveryMessage(Email, "Admin");

        VerifyMessageSent(Times.Never());
    }

    [TestMethod]
    public async Task SendRecoveryMessage_StoreManagerOnAdminPanel_SendsNothing()
    {
        GrantAdminPanel();
        _customer.StaffStoreId = "store";

        await _service.SendRecoveryMessage(Email, "Admin");

        VerifyMessageSent(Times.Never());
    }

    [TestMethod]
    public async Task SendRecoveryMessage_StoreManagerOnStorePanel_SendsWithStoreArea()
    {
        GrantStorePanel();

        await _service.SendRecoveryMessage(Email, "Store");

        _customerService.Verify(x => x.UpdateUserField(_customer, SystemCustomerFieldNames.PasswordRecoveryArea,
            "store", ""), Times.Once);
        VerifyMessageSent(Times.Once());
    }

    [TestMethod]
    public async Task SendRecoveryMessage_VendorOfInactiveVendor_SendsNothing()
    {
        GrantVendorPanel(vendorActive: false);

        await _service.SendRecoveryMessage(Email, "Vendor");

        VerifyMessageSent(Times.Never());
    }

    [TestMethod]
    public async Task SendRecoveryMessage_VendorOnVendorPanel_Sends()
    {
        GrantVendorPanel();

        await _service.SendRecoveryMessage(Email, "Vendor");

        VerifyMessageSent(Times.Once());
    }

    [TestMethod]
    public async Task SendRecoveryMessage_InactiveOrDeletedOrLockedOut_SendsNothing()
    {
        GrantAdminPanel();

        _customer.Active = false;
        await _service.SendRecoveryMessage(Email, "Admin");
        _customer.Active = true;
        _customer.Deleted = true;
        await _service.SendRecoveryMessage(Email, "Admin");
        _customer.Deleted = false;
        _customer.CannotLoginUntilDateUtc = DateTime.UtcNow.AddMinutes(5);
        await _service.SendRecoveryMessage(Email, "Admin");

        VerifyMessageSent(Times.Never());
    }

    [TestMethod]
    public async Task ValidateToken_ValidToken_Null()
    {
        GrantAdminPanel();
        GiveToken(DateTime.UtcNow);

        Assert.IsNull(await _service.ValidateToken(Email, Token, "Admin"));
    }

    [TestMethod]
    public async Task ValidateToken_WrongTokenOrNoPanelAccess_WrongToken()
    {
        GiveToken(DateTime.UtcNow);

        //no access to the admin panel reads the same as a wrong link
        Assert.AreEqual("Account.PasswordRecovery.WrongToken", await _service.ValidateToken(Email, Token, "Admin"));

        GrantAdminPanel();
        Assert.AreEqual("Account.PasswordRecovery.WrongToken", await _service.ValidateToken(Email, "other", "Admin"));
        Assert.AreEqual("Account.PasswordRecovery.WrongToken",
            await _service.ValidateToken("nobody@example.com", Token, "Admin"));
    }

    [TestMethod]
    public async Task ValidateToken_ExpiredLink_LinkExpired()
    {
        GrantAdminPanel();
        GiveToken(DateTime.UtcNow.AddDays(-8));

        Assert.AreEqual("Account.PasswordRecovery.LinkExpired", await _service.ValidateToken(Email, Token, "Admin"));
    }

    [TestMethod]
    public async Task ResetPassword_ValidLink_ChangesPasswordAndClearsLink()
    {
        GrantAdminPanel();
        GiveToken(DateTime.UtcNow);

        var result = await _service.ResetPassword(Email, Token, "Admin", "N3w-password");

        Assert.IsNull(result);
        _customerManagerService.Verify(x => x.ChangePassword(It.Is<ChangePasswordRequest>(r =>
            r.Email == Email && r.NewPassword == "N3w-password"), ""), Times.Once);
        _customerService.Verify(x => x.UpdateUserField<string>(_customer,
            SystemCustomerFieldNames.PasswordRecoveryToken, null, ""), Times.Once);
        _customerService.Verify(x => x.UpdateUserField<string>(_customer,
            SystemCustomerFieldNames.PasswordRecoveryArea, null, ""), Times.Once);
    }

    [TestMethod]
    public async Task ResetPassword_WrongToken_DoesNotChangePassword()
    {
        GrantAdminPanel();
        GiveToken(DateTime.UtcNow);

        var result = await _service.ResetPassword(Email, "other", "Admin", "N3w-password");

        Assert.AreEqual("Account.PasswordRecovery.WrongToken", result);
        _customerManagerService.Verify(x => x.ChangePassword(It.IsAny<ChangePasswordRequest>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ResetPassword_PreviousPassword_Rejected()
    {
        GrantAdminPanel();
        GiveToken(DateTime.UtcNow);
        _customerSettings.UnduplicatedPasswordsNumber = 3;
        _historyService.Setup(x => x.GetPasswords("c1", 3)).ReturnsAsync(new List<CustomerHistoryPassword> {
            new() { Password = "old-hash", PasswordSalt = "" }
        });
        _customerManagerService.Setup(x => x.PasswordMatch(It.IsAny<PasswordFormat>(), "old-hash",
            "Old-password1", "")).Returns(true);

        var result = await _service.ResetPassword(Email, Token, "Admin", "Old-password1");

        Assert.AreEqual("Account.PasswordRecovery.PasswordMatchesWithPrevious", result);
        _customerManagerService.Verify(x => x.ChangePassword(It.IsAny<ChangePasswordRequest>(),
            It.IsAny<string>()), Times.Never);
    }
}
