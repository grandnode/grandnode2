using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Models.Messages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Grand.Web.Admin.Tests.Models;

/// <summary>
///     The flags the server sets for a view (read-only, shared with other stores) must never come from a posted form
/// </summary>
[TestClass]
public class ServerFlagBindingTests
{
    [TestMethod]
    [DataRow(typeof(ProductModel), nameof(ProductModel.IsReadOnly))]
    [DataRow(typeof(ContactAttributeModel), nameof(ContactAttributeModel.IsReadOnly))]
    [DataRow(typeof(MessageTemplateModel), nameof(MessageTemplateModel.IsReadOnly))]
    [DataRow(typeof(AddressAttributeModel), nameof(AddressAttributeModel.IsGlobalAttribute))]
    [DataRow(typeof(CustomerAttributeModel), nameof(CustomerAttributeModel.IsGlobalAttribute))]
    public void ServerFlag_IsNeverBound(Type model, string property)
    {
        Assert.IsNotNull(model.GetProperty(property)!.GetCustomAttribute<BindNeverAttribute>());
    }
}
