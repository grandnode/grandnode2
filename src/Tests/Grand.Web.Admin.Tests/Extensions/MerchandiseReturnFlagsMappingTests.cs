using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Extensions;

/// <summary>
///     The return shipping flags of the reasons and actions reach the forms and come back, mapped by name
/// </summary>
[TestClass]
public class MerchandiseReturnFlagsMappingTests
{
    [TestInitialize]
    public void Init()
    {
        AutoMapperConfig.Init(new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MerchandiseReturnReasonProfile>();
            cfg.AddProfile<MerchandiseReturnActionProfile>();
        }));
    }

    [TestMethod]
    public void Reason_FreeReturnShipping_MappedBothWays()
    {
        Assert.IsTrue(new MerchandiseReturnReason { FreeReturnShipping = true }.ToModel().FreeReturnShipping);
        Assert.IsTrue(new MerchandiseReturnReasonModel { FreeReturnShipping = true }
            .ToEntity(new MerchandiseReturnReason()).FreeReturnShipping);
        Assert.IsFalse(new MerchandiseReturnReasonModel()
            .ToEntity(new MerchandiseReturnReason { FreeReturnShipping = true }).FreeReturnShipping);
    }

    [TestMethod]
    public void Action_WithoutRefund_MappedBothWays()
    {
        Assert.IsTrue(new MerchandiseReturnAction { WithoutRefund = true }.ToModel().WithoutRefund);
        Assert.IsTrue(new MerchandiseReturnActionModel { WithoutRefund = true }
            .ToEntity(new MerchandiseReturnAction()).WithoutRefund);
        Assert.IsFalse(new MerchandiseReturnActionModel()
            .ToEntity(new MerchandiseReturnAction { WithoutRefund = true }).WithoutRefund);
    }
}
