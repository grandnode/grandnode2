using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Messages.Commands.Handlers.Messages;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class GetRecurringPaymentTokensCommandHandlerTests
{
    private GetRecurringPaymentTokensCommandHandler _handler;
    private Dictionary<string, string> _resources;

    public GetRecurringPaymentTokensCommandHandlerTests()
    {
        //resources the language has; a missing one gives the default, or an empty string when asked for that
        _resources = new Dictionary<string, string>();
        var translationService = new Mock<ITranslationService>();
        translationService
            .Setup(x => x.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string key, string _, string defaultValue, bool returnEmpty) =>
                _resources.TryGetValue(key, out var value) ? value : returnEmpty ? string.Empty : defaultValue);
        _handler = new GetRecurringPaymentTokensCommandHandler(translationService.Object);
    }

    private static GetRecurringPaymentTokensCommand Command(RecurringPayment recurringPayment)
    {
        return new GetRecurringPaymentTokensCommand {
            RecurringPayment = recurringPayment,
            Language = new Language { Id = "lang1" }
        };
    }

    [TestMethod]
    public async Task Tokens_TranslateThePeriodAndEncodeWhatOthersTyped()
    {
        _resources["Enums.Grand.Domain.Catalog.RecurringCyclePeriod.Months"] = "Monate";
        var recurringPayment = new RecurringPayment {
            CycleLength = 1,
            CyclePeriodId = RecurringCyclePeriod.Months,
            CyclesPaid = 3,
            TotalCycles = 12,
            LastError = "<b>card</b>",
            CancelledBy = "O'Neil & co"
        };

        var tokens = await _handler.Handle(Command(recurringPayment), default);

        Assert.AreEqual(1, tokens.CycleLength);
        Assert.AreEqual("Monate", tokens.CyclePeriod);
        Assert.AreEqual(3, tokens.CyclesPaid);
        Assert.AreEqual(12, tokens.TotalCycles);
        Assert.AreEqual("&lt;b&gt;card&lt;/b&gt;", tokens.LastError);
        Assert.AreEqual("O&#39;Neil &amp; co", tokens.CancelledBy);
    }

    [TestMethod]
    public async Task Tokens_NullTextsAreEmpty()
    {
        var recurringPayment = new RecurringPayment { LastError = null, CancelledBy = null };

        var tokens = await _handler.Handle(Command(recurringPayment), default);

        Assert.AreEqual(string.Empty, tokens.LastError);
        Assert.AreEqual(string.Empty, tokens.CancelledBy);
    }
}
