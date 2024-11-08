using Grand.Domain.Localization;
using Grand.SharedKernel.Extensions;
using System.Xml;
using System.Xml.Schema;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallLocaleResources()
    {
        //'English' language
        var language = _languageRepository.Table.Single(l => l.Name == "English");

        //save resources
        var filePath = Path.Combine(_hostingEnvironment.ContentRootPath, "App_Data/Resources/DefaultLanguage.xml");
        var localesXml = File.ReadAllText(filePath);

        var xmlDoc = LanguageXmlDocument(localesXml);

        var translateResources = new List<TranslationResource>();

        var nodes = xmlDoc.SelectNodes("//Language/Resource");
        if (nodes != null)
            foreach (XmlNode node in nodes)
            {
                var name = node.Attributes?["Name"]?.InnerText.Trim();
                var area = node.Attributes?["Area"]?.InnerText.Trim();
                var value = "";
                var valueNode = node.SelectSingleNode("Value");
                if (valueNode != null)
                    value = valueNode.InnerText;

                if (string.IsNullOrEmpty(name))
                    continue;

                _ = Enum.TryParse(area, out TranslationResourceArea areaEnum);

                await _lsrRepository.InsertAsync(new TranslationResource {
                    LanguageId = language.Id,
                    Name = name.ToLowerInvariant(),
                    Value = value,
                    Area = areaEnum,
                    CreatedBy = "System"
                });
            }

    }
    private static XmlDocument LanguageXmlDocument(string xml)
    {
        var schemas = new XmlSchemaSet();
        schemas.Add("", XmlReader.Create(new StringReader(LanguageSchema.SchemaXsd)));

        var xmlDoc = new XmlDocument { Schemas = schemas, XmlResolver = null };
        xmlDoc.LoadXml(xml);

        // Validate XML.
        xmlDoc.Validate((_, e) => throw new XmlException("XML data does not conform to the schema", e.Exception));
        return xmlDoc;
    }
}