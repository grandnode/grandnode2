using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Messages;
using Grand.Module.Installer.Services.MessageTemplates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

[TestClass]
public class OrderApprovalTemplateSeedTests
{
    private static MessageTemplate Template(string name)
    {
        return MessageTemplateSeed.Build("ea").Single(t => t.Name == name);
    }

    [TestMethod]
    [DataRow("OrderApproval.Requested")]
    [DataRow("OrderApproval.Decided")]
    public void OrderApprovalTemplates_AreActiveAndUseOnlyBackedTokens(string name)
    {
        var template = Template(name);
        Assert.IsTrue(template.IsActive);

        var backed = LiquidExtensions.GetTokens(typeof(LiquidOrderApproval), typeof(LiquidCustomer), typeof(LiquidStore))
            .Select(x => x.Trim('{', '}')).ToHashSet();
        var used = MessageTemplateSeedTests.Tokens(template.Subject + template.Body)
            .Where(x => x.StartsWith("OrderApproval.") || x.StartsWith("Customer.") || x.StartsWith("Store."));
        foreach (var token in used)
            Assert.IsTrue(backed.Contains(token), $"{name} uses {token}, which no drop provides");
    }

    [TestMethod]
    public void Requested_RendersEncodedBuyerAndItemsHtml()
    {
        var liquidObject = new LiquidObject {
            OrderApproval = new LiquidOrderApproval {
                BuyerName = "A <b>& B",
                BuyerEmail = "buyer@example.com",
                Total = "EUR 120.00",
                ItemsHtml = "<table>ITEMS</table>",
                Url = "https://store.example/account/orderapproval?id=1"
            }
        };

        var subject = LiquidExtensions.Render(liquidObject, Template("OrderApproval.Requested").Subject);
        var body = LiquidExtensions.Render(liquidObject, Template("OrderApproval.Requested").Body);

        StringAssert.Contains(subject, "A <b>& B");
        StringAssert.Contains(body, "A &lt;b&gt;&amp; B");
        Assert.IsFalse(body.Contains("A <b>"));
        StringAssert.Contains(body, "<table>ITEMS</table>");
        StringAssert.Contains(body, "href='https://store.example/account/orderapproval?id=1'");
    }

    [TestMethod]
    public void Requested_ShowsTheOrderNumber()
    {
        var body = LiquidExtensions.Render(
            new LiquidObject { OrderApproval = new LiquidOrderApproval { OrderNumber = "1042" } },
            Template("OrderApproval.Requested").Body);

        StringAssert.Contains(body, "1042");
    }

    [TestMethod]
    public void Decided_LinksToTheOrderNotTheCart()
    {
        var template = Template("OrderApproval.Decided");
        var body = LiquidExtensions.Render(
            new LiquidObject {
                OrderApproval = new LiquidOrderApproval { OrderUrl = "https://store.example/orderdetails/o1" }
            }, template.Body);

        StringAssert.Contains(body, "href='https://store.example/orderdetails/o1'");
        Assert.IsFalse(body.Contains("/cart"));
        Assert.IsFalse(template.Body.Contains("OrderApproval.CartUrl"));
    }

    [TestMethod]
    [DataRow("OrderApproval.Requested")]
    [DataRow("OrderApproval.Decided")]
    public void Subject_HasNoEmDash(string name)
    {
        Assert.IsFalse(Template(name).Subject.Contains('—'));
    }

    [TestMethod]
    public void Decided_ShowsCommentOnlyWhenPresent()
    {
        var withComment = LiquidExtensions.Render(
            new LiquidObject { OrderApproval = new LiquidOrderApproval { DecisionComment = "Too expensive" } },
            Template("OrderApproval.Decided").Body);
        var withoutComment = LiquidExtensions.Render(
            new LiquidObject { OrderApproval = new LiquidOrderApproval { DecisionComment = "" } },
            Template("OrderApproval.Decided").Body);

        StringAssert.Contains(withComment, "Too expensive");
        Assert.IsFalse(withoutComment.Contains("Comment from the approver"));
    }
}
