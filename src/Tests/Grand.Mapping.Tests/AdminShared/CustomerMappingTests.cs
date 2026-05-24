using Grand.Mapping;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Models.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class CustomerMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<CustomerGroupProfile>();
            cfg.AddProfile<CustomerAttributeProfile>();
            cfg.AddProfile<CustomerAttributeValueProfile>();
            cfg.AddProfile<CustomerTagProfile>();
            cfg.AddProfile<SalesEmployeeProfile>();
            cfg.AddProfile<ContactAttributeProfile>();
            cfg.AddProfile<ContactUsProfile>();
            cfg.AddProfile<NewsLetterSubscriptionProfile>();
            cfg.AddProfile<NewsletterCategoryProfile>();
            cfg.AddProfile<UserApiProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── CustomerGroup ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task CustomerGroup_ToModel()
    {
        var source = new CustomerGroup {
            Id = "cg-001",
            Name = "VIP Customers",
            FreeShipping = true,
            TaxExempt = false,
            Active = true,
            IsSystem = false,
            SystemName = "vip",
            EnablePasswordLifetime = false
        };
        return Verify(_mapper.Map<CustomerGroupModel>(source));
    }

    [TestMethod]
    public Task CustomerGroupModel_ToDomain()
    {
        var model = new CustomerGroupModel {
            Name = "VIP Customers",
            FreeShipping = true,
            TaxExempt = false,
            Active = true,
            SystemName = "vip"
        };
        return Verify(_mapper.Map<CustomerGroup>(model));
    }

    // ── CustomerAttribute ─────────────────────────────────────────────────────

    [TestMethod]
    public Task CustomerAttribute_ToModel()
    {
        var source = new CustomerAttribute {
            Id = "ca-001",
            Name = "Newsletter Preference",
            IsRequired = false,
            AttributeControlTypeId = (AttributeControlType)2,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<CustomerAttributeModel>(source));
    }

    [TestMethod]
    public Task CustomerAttributeModel_ToDomain()
    {
        var model = new CustomerAttributeModel {
            Name = "Newsletter Preference",
            IsRequired = false,
            AttributeControlTypeId = 2,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<CustomerAttribute>(model));
    }

    // ── CustomerAttributeValue ────────────────────────────────────────────────

    [TestMethod]
    public Task CustomerAttributeValue_ToModel()
    {
        var source = new CustomerAttributeValue {
            Id = "cav-001",
            Name = "Weekly",
            IsPreSelected = false,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<CustomerAttributeValueModel>(source));
    }

    [TestMethod]
    public Task CustomerAttributeValueModel_ToDomain()
    {
        var model = new CustomerAttributeValueModel {
            Name = "Weekly",
            IsPreSelected = false,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<CustomerAttributeValue>(model));
    }

    // ── CustomerTag ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task CustomerTag_ToModel()
    {
        var source = new CustomerTag { Id = "ct-001", Name = "BigSpender" };
        return Verify(_mapper.Map<CustomerTagModel>(source));
    }

    [TestMethod]
    public Task CustomerTagModel_ToDomain()
    {
        var model = new CustomerTagModel { Name = "BigSpender" };
        return Verify(_mapper.Map<CustomerTag>(model));
    }

    // ── SalesEmployee ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task SalesEmployee_ToModel()
    {
        var source = new SalesEmployee {
            Id = "se-001",
            Name = "Alice Smith",
            Active = true,
            Commission = 5.0,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<SalesEmployeeModel>(source));
    }

    [TestMethod]
    public Task SalesEmployeeModel_ToDomain()
    {
        var model = new SalesEmployeeModel {
            Name = "Alice Smith",
            Active = true,
            Commission = 5.0,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<SalesEmployee>(model));
    }

    // ── ContactAttribute ──────────────────────────────────────────────────────

    [TestMethod]
    public Task ContactAttribute_ToModel()
    {
        var source = new ContactAttribute {
            Id = "coa-001",
            Name = "Preferred Contact",
            TextPrompt = "How would you like to be contacted?",
            IsRequired = false,
            AttributeControlTypeId = 1,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<ContactAttributeModel>(source));
    }

    [TestMethod]
    public Task ContactAttributeModel_ToDomain()
    {
        var model = new ContactAttributeModel {
            Name = "Preferred Contact",
            IsRequired = false,
            AttributeControlTypeId = 1,
            DisplayOrder = 1,
            CustomerGroups = ["grp-001"],
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<ContactAttribute>(model));
    }

    // ── ContactAttributeValue ─────────────────────────────────────────────────

    [TestMethod]
    public Task ContactAttributeValue_ToModel()
    {
        var source = new ContactAttributeValue {
            Id = "coav-001",
            Name = "Email",
            IsPreSelected = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<ContactAttributeValueModel>(source));
    }

    [TestMethod]
    public Task ContactAttributeValueModel_ToDomain()
    {
        var model = new ContactAttributeValueModel {
            Name = "Email",
            IsPreSelected = true,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<ContactAttributeValue>(model));
    }

    // ── ContactUs ─────────────────────────────────────────────────────────────

    [TestMethod]
    public Task ContactUs_ToModel()
    {
        var source = new ContactUs {
            Id = "cu-001",
            Email = "customer@example.com",
            FullName = "Jane Doe",
            Subject = "Order inquiry",
            Enquiry = "When will my order arrive?",
            EmailAccountId = "ea-001",
            StoreId = "store-001",
            IpAddress = "192.168.1.1"
        };
        return Verify(_mapper.Map<ContactFormModel>(source));
    }

    // ── NewsLetterSubscription ────────────────────────────────────────────────

    [TestMethod]
    public Task NewsLetterSubscription_ToModel()
    {
        var source = new NewsLetterSubscription {
            Id = "nls-001",
            Email = "subscriber@example.com",
            Active = true,
            StoreId = "store-001"
        };
        return Verify(_mapper.Map<NewsLetterSubscriptionModel>(source));
    }

    [TestMethod]
    public Task NewsLetterSubscriptionModel_ToDomain()
    {
        var model = new NewsLetterSubscriptionModel {
            Email = "subscriber@example.com",
            Active = true
        };
        return Verify(_mapper.Map<NewsLetterSubscription>(model));
    }

    // ── NewsletterCategory ────────────────────────────────────────────────────

    [TestMethod]
    public Task NewsletterCategory_ToModel()
    {
        var source = new NewsletterCategory {
            Id = "nlc-001",
            Name = "Tech Updates",
            Description = "Technology newsletter",
            Selected = false,
            DisplayOrder = 1
        };
        return Verify(_mapper.Map<NewsletterCategoryModel>(source));
    }

    [TestMethod]
    public Task NewsletterCategoryModel_ToDomain()
    {
        var model = new NewsletterCategoryModel {
            Name = "Tech Updates",
            Description = "Technology newsletter",
            DisplayOrder = 1,
            Stores = ["store-001"]
        };
        return Verify(_mapper.Map<NewsletterCategory>(model));
    }

    // ── UserApi ───────────────────────────────────────────────────────────────

    [TestMethod]
    public Task UserApi_ToModel()
    {
        var source = new UserApi {
            Id = "ua-001",
            Email = "api@example.com",
            IsActive = true
        };
        return Verify(_mapper.Map<UserApiModel>(source));
    }

    [TestMethod]
    public Task UserApiModel_ToDomain()
    {
        var model = new UserApiModel {
            Email = "api@example.com",
            IsActive = true
        };
        return Verify(_mapper.Map<UserApi>(model));
    }

    [TestMethod]
    public Task UserApiCreateModel_ToDomain()
    {
        var model = new UserApiCreateModel {
            Email = "newapi@example.com",
            Password = "Secret123!",
            IsActive = true
        };
        return Verify(_mapper.Map<UserApi>(model));
    }
}
