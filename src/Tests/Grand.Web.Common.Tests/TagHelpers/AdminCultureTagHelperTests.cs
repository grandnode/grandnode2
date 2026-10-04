using System.Globalization;
using System.Text.Json;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.Common.TagHelpers.Admin;
using Grand.Web.Common.TagHelpers.Admin.Grid;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.TagHelpers;

/// <summary>
///     The island the admin widgets read their culture from. What it carries is what decides
///     whether a date is rendered and parsed in the store's culture or in the browser's locale.
/// </summary>
[TestClass]
public class AdminCultureTagHelperTests
{
    private Mock<IContextAccessor> _contextAccessorMock;
    private CultureInfo _originalCulture;
    private Mock<ITranslationService> _translationServiceMock;
    private Language _workingLanguage;

    [TestInitialize]
    public void Init()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        _workingLanguage = new Language { Id = "lang1" };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(x => x.WorkingLanguage).Returns(() => _workingLanguage);
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(x => x.WorkContext).Returns(workContextMock.Object);
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock
            .Setup(x => x.GetResource(It.IsAny<string>(), "lang1", string.Empty, true))
            .Returns((string key, string _, string _, bool _) => $"[{key}]");
    }

    [TestCleanup]
    public void Cleanup()
    {
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private JsonElement Run()
    {
        var helper = new AdminCultureTagHelper(_translationServiceMock.Object, _contextAccessorMock.Object);
        var output = new TagHelperOutput("admin-culture",
            [],
            (_, _) => Task.FromResult<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent>(new DefaultTagHelperContent()));
        helper.Process(new TagHelperContext([], new Dictionary<object, object>(), "id"), output);
        using var writer = new StringWriter();
        output.Content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return JsonDocument.Parse(writer.ToString()).RootElement.Clone();
    }

    [TestMethod]
    public void Process_WritesTheRequestCulture()
    {
        CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
        var json = Run();

        Assert.AreEqual("pl-PL", json.GetProperty("name").GetString());
        Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern,
            json.GetProperty("calendar").GetProperty("shortDate").GetString());
    }

    [TestMethod]
    public void Process_CarriesTheDayTheWeekStartsOn()
    {
        //Monday in Poland - a calendar cannot guess it, and the browser's locale is the wrong
        //place to ask. The invariant defaults of format.js start the week on Sunday.
        CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
        Assert.AreEqual((int)DayOfWeek.Monday,
            Run().GetProperty("calendar").GetProperty("firstDayOfWeek").GetInt32());

        //whatever the machine's globalization data says for another culture, it is that
        //culture's own answer that travels, never a constant
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        Assert.AreEqual((int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek,
            Run().GetProperty("calendar").GetProperty("firstDayOfWeek").GetInt32());
    }

    [TestMethod]
    public void Process_CarriesBothSpellingsOfAMonthName()
    {
        CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
        var calendar = Run().GetProperty("calendar");

        //a date is written with the genitive ("16 września"), a calendar heading is not
        Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.MonthGenitiveNames[8],
            calendar.GetProperty("months")[8].GetString());
        Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[8],
            calendar.GetProperty("monthsStandalone")[8].GetString());
    }

    [TestMethod]
    public void Process_TranslatesTheWidgetTexts()
    {
        var texts = Run().GetProperty("texts");

        foreach (var (name, key) in AdminCultureTagHelper.TextResources)
            Assert.AreEqual($"[{key}]", texts.GetProperty(name).GetString(), name);
    }

    [TestMethod]
    public void Process_LeavesOutAResourceTheInstallationNeverImported()
    {
        _translationServiceMock
            .Setup(x => x.GetResource("Admin.Common.Today", "lang1", string.Empty, true))
            .Returns(string.Empty);

        var texts = Run().GetProperty("texts");

        //the widget keeps its own neutral default rather than showing the raw resource key
        Assert.IsFalse(texts.TryGetProperty("today", out _));
        Assert.AreEqual("[Admin.Common.Clear]", texts.GetProperty("clear").GetString());
    }

    [TestMethod]
    public void Process_WithoutAWorkingLanguage_WritesTheCultureAndNoTexts()
    {
        _workingLanguage = null;
        var json = Run();

        Assert.AreEqual(0, json.GetProperty("texts").EnumerateObject().Count());
        Assert.AreEqual("en-US", json.GetProperty("name").GetString());
    }

    [TestMethod]
    public void GridCulture_FirstDayOfWeek_MatchesTheCulture()
    {
        Assert.AreEqual((int)new CultureInfo("ar-SA").DateTimeFormat.FirstDayOfWeek,
            GridCulture.From(new CultureInfo("ar-SA")).Calendar.FirstDayOfWeek);
    }
}
