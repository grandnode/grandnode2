using Grand.Mapping;
using Grand.Domain.Admin;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Domain.Tasks;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Models.Menu;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.AdminShared.Models.Tasks;
using Grand.Web.AdminShared.Models.Vendors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DomainVendor = Grand.Domain.Vendors.Vendor;
using VendorSettings = Grand.Domain.Vendors.VendorSettings;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class SystemMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<EmailAccountProfile>();
            cfg.AddProfile<QueuedEmailProfile>();
            cfg.AddProfile<MessageTemplateProfile>();
            cfg.AddProfile<CampaignProfile>();
            cfg.AddProfile<MenuProfile>();
            cfg.AddProfile<ScheduleTaskProfile>();
            cfg.AddProfile<VendorProfile>();
            cfg.AddProfile<VendorSettingsProfile>();
            cfg.AddProfile<AddressProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── EmailAccount ──────────────────────────────────────────────────────────

    [TestMethod]
    public Task EmailAccount_ToModel()
    {
        var source = new EmailAccount {
            Id = "ea-001",
            Email = "noreply@store.com",
            DisplayName = "Grand Store",
            Host = "smtp.example.com",
            Port = 587,
            Username = "noreply@store.com"
        };
        return Verify(_mapper.Map<EmailAccountModel>(source));
    }

    [TestMethod]
    public Task EmailAccountModel_ToDomain()
    {
        var model = new EmailAccountModel {
            Email = "noreply@store.com",
            DisplayName = "Grand Store",
            Host = "smtp.example.com",
            Port = 587,
            Username = "noreply@store.com"
        };
        return Verify(_mapper.Map<EmailAccount>(model));
    }

    // ── QueuedEmail ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task QueuedEmail_ToModel()
    {
        var source = new QueuedEmail {
            Id = "qe-001",
            PriorityId = QueuedEmailPriority.High,
            From = "from@store.com",
            FromName = "Store",
            To = "customer@example.com",
            ToName = "Customer",
            ReplyTo = "",
            CC = "",
            Bcc = "",
            Subject = "Order Confirmation",
            Body = "<p>Thank you!</p>",
            SentTries = 0
        };
        return Verify(_mapper.Map<QueuedEmailModel>(source));
    }

    [TestMethod]
    public Task QueuedEmailModel_ToDomain()
    {
        var model = new QueuedEmailModel {
            From = "from@store.com",
            To = "customer@example.com",
            Subject = "Order Confirmation",
            Body = "<p>Thank you!</p>"
        };
        return Verify(_mapper.Map<QueuedEmail>(model));
    }

    // ── MessageTemplate ───────────────────────────────────────────────────────

    [TestMethod]
    public Task MessageTemplate_ToModel()
    {
        var source = new MessageTemplate {
            Id = "mt-001",
            Name = "OrderPlaced.CustomerNotification",
            BccEmailAddresses = "",
            Subject = "Your order has been placed",
            Body = "Order body",
            IsActive = true,
            EmailAccountId = "ea-001",
            DelayBeforeSend = 0,
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<MessageTemplateModel>(source));
    }

    [TestMethod]
    public Task MessageTemplateModel_ToDomain()
    {
        var model = new MessageTemplateModel {
            Name = "OrderPlaced.CustomerNotification",
            Subject = "Your order has been placed",
            Body = "Order body",
            IsActive = true,
            EmailAccountId = "ea-001",
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<MessageTemplate>(model));
    }

    // ── Campaign ──────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Campaign_ToModel()
    {
        var source = new Campaign {
            Id = "camp-001",
            Name = "Summer Campaign",
            Subject = "Summer Sale!",
            Body = "Check our summer deals",
            StoreId = "store-001"
        };
        return Verify(_mapper.Map<CampaignModel>(source));
    }

    [TestMethod]
    public Task CampaignModel_ToDomain()
    {
        var model = new CampaignModel {
            Name = "Summer Campaign",
            Subject = "Summer Sale!",
            Body = "Check our summer deals",
            StoreId = "store-001"
        };
        return Verify(_mapper.Map<Campaign>(model));
    }

    // ── AdminSiteMap (Menu) ───────────────────────────────────────────────────

    [TestMethod]
    public Task AdminSiteMap_ToMenuModel()
    {
        var source = new AdminSiteMap {
            Id = "menu-001",
            SystemName = "Dashboard",
            ResourceName = "Admin.Dashboard",
            ControllerName = "Admin",
            ActionName = "Dashboard",
            IconClass = "fa fa-home",
            DisplayOrder = 1,
            Url = "/Admin/Dashboard"
        };
        return Verify(_mapper.Map<MenuModel>(source));
    }

    [TestMethod]
    public Task MenuModel_ToAdminSiteMap()
    {
        var model = new MenuModel {
            Id = "menu-001",
            SystemName = "Dashboard",
            ResourceName = "Admin.Dashboard",
            ControllerName = "Admin",
            ActionName = "Dashboard",
            IconClass = "fa fa-home",
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<AdminSiteMap>(model));
    }

    // ── ScheduleTask ──────────────────────────────────────────────────────────

    [TestMethod]
    public Task ScheduleTask_ToModel()
    {
        var source = new ScheduleTask {
            Id = "st-001",
            ScheduleTaskName = "Grand.Services.Catalog.UpdateExchangeRateTask, Grand.Services",
            Enabled = true,
            StopOnError = false,
            TimeInterval = 60,
            StoreId = ""
        };
        return Verify(_mapper.Map<ScheduleTaskModel>(source));
    }

    [TestMethod]
    public Task ScheduleTaskModel_ToDomain()
    {
        var model = new ScheduleTaskModel {
            TimeInterval = 60,
            Enabled = true,
            StopOnError = false
        };
        return Verify(_mapper.Map<ScheduleTask>(model));
    }

    // ── Vendor ────────────────────────────────────────────────────────────────

    [TestMethod]
    public Task Vendor_ToModel()
    {
        var source = new DomainVendor {
            Id = "vendor-001",
            Name = "Best Vendor",
            Email = "vendor@example.com",
            Description = "Top vendor",
            AdminComment = "Internal note",
            PictureId = "pic-vendor",
            PageSize = 10,
            AllowCustomersToSelectPageSize = false,
            PageSizeOptions = "10, 20",
            Active = true,
            DisplayOrder = 1,
            SeName = "best-vendor",
            AllowCustomerReviews = true
        };
        return Verify(_mapper.Map<VendorModel>(source));
    }

    [TestMethod]
    public Task VendorModel_ToDomain()
    {
        var model = new VendorModel {
            Name = "Best Vendor",
            Email = "vendor@example.com",
            Description = "Top vendor",
            Active = true
        };
        return Verify(_mapper.Map<DomainVendor>(model));
    }

    // ── VendorSettings ────────────────────────────────────────────────────────

    [TestMethod]
    public Task VendorSettings_ToModel()
    {
        var source = new VendorSettings {
            VendorsBlockItemsToDisplay = 3,
            ShowVendorOnProductDetailsPage = true,
            AllowCustomersToContactVendors = true,
            AllowCustomersToApplyForVendorAccount = false,
            AllowSearchByVendor = true,
            AllowVendorsToEditInfo = false,
            NotifyStoreOwnerAboutVendorInformationChange = true
        };
        return Verify(_mapper.Map<VendorSettingsModel>(source));
    }

    [TestMethod]
    public Task VendorSettingsModel_ToDomain()
    {
        var model = new VendorSettingsModel {
            VendorsBlockItemsToDisplay = 3,
            ShowVendorOnProductDetailsPage = true,
            AllowCustomersToContactVendors = true
        };
        return Verify(_mapper.Map<VendorSettings>(model));
    }

    [TestMethod]
    public Task VendorSettings_ToModel_WithAddressSettings()
    {
        var source = new VendorSettings {
            VendorsBlockItemsToDisplay = 5,
            CityEnabled = true,
            CityRequired = false,
            CompanyEnabled = true,
            CompanyRequired = true,
            CountryEnabled = true,
            FaxEnabled = false,
            FaxRequired = false,
            PhoneEnabled = true,
            PhoneRequired = false,
            StateProvinceEnabled = true,
            StreetAddress2Enabled = true,
            StreetAddress2Required = false,
            StreetAddressEnabled = true,
            StreetAddressRequired = true,
            ZipPostalCodeEnabled = true,
            ZipPostalCodeRequired = false
        };
        return Verify(_mapper.Map<VendorSettingsModel>(source));
    }

    [TestMethod]
    public Task VendorSettingsModel_ToDomain_WithAddressSettings()
    {
        var model = new VendorSettingsModel {
            VendorsBlockItemsToDisplay = 5,
            AddressSettings = new VendorSettingsModel.AddressSettingsModel {
                CityEnabled = true,
                CityRequired = false,
                CompanyEnabled = true,
                CompanyRequired = true,
                CountryEnabled = true,
                FaxEnabled = false,
                FaxRequired = false,
                PhoneEnabled = true,
                PhoneRequired = false,
                StateProvinceEnabled = true,
                StreetAddress2Enabled = true,
                StreetAddress2Required = false,
                StreetAddressEnabled = true,
                StreetAddressRequired = true,
                ZipPostalCodeEnabled = true,
                ZipPostalCodeRequired = false
            }
        };
        return Verify(_mapper.Map<VendorSettings>(model));
    }
}
