using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;

namespace Grand.SharedKernel.Tests.Extensions;

[TestClass]
public class XmlExtensionsTests
{
    [TestMethod]
    public void LanguageXmlDocument_LanguageFile_Loaded()
    {
        var xml = """
                  <?xml version="1.0" encoding="utf-8"?>
                  <Language Name="English">
                    <Resource Name="Admin.Test" Area="Admin">
                      <Value>Test</Value>
                    </Resource>
                  </Language>
                  """;

        var resources = XmlExtensions.ParseTranslationResources(XmlExtensions.LanguageXmlDocument(xml));

        Assert.AreEqual("Test", resources.Single().Value);
    }

    [TestMethod]
    public void LanguageXmlDocument_WithDtd_Refused()
    {
        //entities that expand into each other would take the memory of the server ("billion laughs")
        var xml = """
                  <?xml version="1.0"?>
                  <!DOCTYPE Language [<!ENTITY a "aaaaaaaaaa"><!ENTITY b "&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;">]>
                  <Language Name="English"><Resource Name="x" Area="Admin"><Value>&b;</Value></Resource></Language>
                  """;

        Assert.ThrowsExactly<XmlException>(() => XmlExtensions.LanguageXmlDocument(xml));
    }
}
