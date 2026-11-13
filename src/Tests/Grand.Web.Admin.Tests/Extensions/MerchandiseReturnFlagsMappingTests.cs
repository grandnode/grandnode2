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
///     The return shipping flag of the reasons reaches the form and comes back, mapped by name
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
}
